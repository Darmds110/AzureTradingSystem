using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text;
using TradingSystem.Functions.Models;
using TradingSystem.Functions.Services.Interfaces;

namespace TradingSystem.Functions.Functions
{
    /// <summary>
    /// Azure Function that sends daily portfolio summary email
    /// Runs daily after market close (5:30 PM ET / 22:30 UTC)
    /// </summary>
    public class DailyPortfolioSummary
    {
        private readonly ILogger<DailyPortfolioSummary> _logger;
        private readonly IPortfolioService _portfolioService;
        private readonly IPerformanceService _performanceService;
        private readonly IAlpacaAccountService _alpacaService;
        private readonly IEmailService _emailService;

        public DailyPortfolioSummary(
            ILogger<DailyPortfolioSummary> logger,
            IPortfolioService portfolioService,
            IPerformanceService performanceService,
            IAlpacaAccountService alpacaService,
            IEmailService emailService)
        {
            _logger = logger;
            _portfolioService = portfolioService;
            _performanceService = performanceService;
            _alpacaService = alpacaService;
            _emailService = emailService;
        }

        /// <summary>
        /// Timer trigger that runs daily at 5:30 PM ET (22:30 UTC)
        /// CRON: "0 30 22 * * 1-5" = At 22:30 UTC, Monday through Friday
        /// </summary>
        [Function("DailyPortfolioSummary")]
        public async Task Run([TimerTrigger("0 30 22 * * 1-5")] TimerInfo myTimer)
        {
            _logger.LogInformation("DailyPortfolioSummary triggered at: {time}", DateTime.UtcNow);

            try
            {
                var portfolio = await _portfolioService.GetCurrentPortfolioAsync();
                if (portfolio == null)
                {
                    _logger.LogError("No portfolio found. Skipping daily summary.");
                    return;
                }

                var todayMetrics = await _performanceService.GetLatestMetricsAsync(portfolio.PortfolioId);
                var positions = await _alpacaService.GetPositionsAsync();

                var today = DateTime.UtcNow.Date;
                var weekStart = today.AddDays(-(int)today.DayOfWeek);
                var monthStart = new DateTime(today.Year, today.Month, 1);

                var todayStats = await _performanceService.GetTradeStatisticsAsync(portfolio.PortfolioId, today, today);
                var weekStats = await _performanceService.GetTradeStatisticsAsync(portfolio.PortfolioId, weekStart, today);
                var monthStats = await _performanceService.GetTradeStatisticsAsync(portfolio.PortfolioId, monthStart, today);

                var benchmarks = await _performanceService.CompareToBenchmarksAsync(portfolio.PortfolioId, today);

                var emailBody = BuildDailySummaryEmail(portfolio, todayMetrics, positions,
                    todayStats, weekStats, monthStats, benchmarks);

                // Use PeriodReturnPercent instead of DailyReturnPercent
                var dailyReturn = todayMetrics?.PeriodReturnPercent ?? 0;
                var emoji = dailyReturn >= 0 ? "📈" : "📉";
                var subject = $"{emoji} Daily Portfolio Summary: {dailyReturn:+0.00;-0.00}% | ${portfolio.CurrentEquity:N2}";

                await _emailService.SendSummaryEmailAsync(subject, emailBody);

                _logger.LogInformation("Daily summary email sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating daily portfolio summary");

                try
                {
                    await _emailService.SendAlertAsync(
                        "Daily Summary Failed",
                        $"Failed to generate daily portfolio summary:\n\n{ex.Message}",
                        "MEDIUM");
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to send error notification");
                }

                throw;
            }
        }

        private string BuildDailySummaryEmail(
            Portfolio portfolio,
            PerformanceMetrics? todayMetrics,
            List<PositionInfo> positions,
            TradeStatistics todayStats,
            TradeStatistics weekStats,
            TradeStatistics monthStats,
            BenchmarkComparison benchmarks)
        {
            var sb = new StringBuilder();
            var today = DateTime.UtcNow;

            sb.AppendLine($"<h2>📊 Daily Portfolio Summary - {today:dddd, MMMM d, yyyy}</h2>");
            sb.AppendLine("<hr/>");

            // Portfolio Overview
            sb.AppendLine("<h3>💰 Portfolio Overview</h3>");
            sb.AppendLine("<table style='border-collapse: collapse; width: 100%;'>");
            sb.AppendLine($"<tr><td><strong>Current Value:</strong></td><td style='text-align: right;'><strong>${portfolio.CurrentEquity:N2}</strong></td></tr>");
            sb.AppendLine($"<tr><td>Cash Balance:</td><td style='text-align: right;'>${portfolio.CurrentCash:N2}</td></tr>");
            sb.AppendLine($"<tr><td>Buying Power:</td><td style='text-align: right;'>${portfolio.BuyingPower:N2}</td></tr>");
            sb.AppendLine($"<tr><td>Peak Value:</td><td style='text-align: right;'>${portfolio.PeakValue:N2}</td></tr>");
            sb.AppendLine($"<tr><td>Initial Capital:</td><td style='text-align: right;'>${portfolio.InitialCapital:N2}</td></tr>");
            sb.AppendLine("</table>");

            // Today's Performance - use PeriodReturnPercent
            sb.AppendLine("<h3>📈 Today's Performance</h3>");
            var dailyReturn = todayMetrics?.PeriodReturnPercent ?? 0;
            var dailyColor = dailyReturn >= 0 ? "green" : "red";
            sb.AppendLine($"<p style='font-size: 24px; color: {dailyColor};'><strong>{dailyReturn:+0.00;-0.00}%</strong></p>");

            if (todayMetrics != null)
            {
                sb.AppendLine("<table style='border-collapse: collapse; width: 100%;'>");
                sb.AppendLine($"<tr><td>Total Return (All Time):</td><td style='text-align: right; color: {(todayMetrics.TotalReturnPercent >= 0 ? "green" : "red")};'>{todayMetrics.TotalReturnPercent:+0.00;-0.00}%</td></tr>");
                sb.AppendLine($"<tr><td>Current Drawdown:</td><td style='text-align: right; color: red;'>{portfolio.CurrentDrawdownPercent:0.00}%</td></tr>");
                sb.AppendLine("</table>");
            }

            // Trading Status
            sb.AppendLine("<h3>⚠️ Trading Status</h3>");
            if (portfolio.IsTradingPaused)
            {
                sb.AppendLine($"<p style='color: red; font-weight: bold;'>🛑 TRADING HALTED</p>");
                sb.AppendLine($"<p>Reason: {portfolio.PausedReason}</p>");
            }
            else
            {
                sb.AppendLine("<p style='color: green;'>✅ Trading Active</p>");
            }

            // Current Positions
            sb.AppendLine("<h3>📋 Current Positions</h3>");
            if (positions.Any())
            {
                sb.AppendLine("<table style='border-collapse: collapse; width: 100%; border: 1px solid #ddd;'>");
                sb.AppendLine("<tr style='background-color: #f5f5f5;'><th style='padding: 8px; text-align: left;'>Symbol</th><th style='text-align: right; padding: 8px;'>Qty</th><th style='text-align: right; padding: 8px;'>Price</th><th style='text-align: right; padding: 8px;'>P/L</th><th style='text-align: right; padding: 8px;'>P/L %</th></tr>");

                foreach (var pos in positions.OrderByDescending(p => Math.Abs(p.UnrealizedPL)))
                {
                    var plColor = pos.UnrealizedPL >= 0 ? "green" : "red";
                    sb.AppendLine($"<tr>");
                    sb.AppendLine($"<td style='padding: 8px;'><strong>{pos.Symbol}</strong></td>");
                    sb.AppendLine($"<td style='text-align: right; padding: 8px;'>{pos.Quantity}</td>");
                    sb.AppendLine($"<td style='text-align: right; padding: 8px;'>${pos.CurrentPrice:N2}</td>");
                    sb.AppendLine($"<td style='text-align: right; padding: 8px; color: {plColor};'>${pos.UnrealizedPL:N2}</td>");
                    sb.AppendLine($"<td style='text-align: right; padding: 8px; color: {plColor};'>{pos.UnrealizedPLPercent * 100:+0.00;-0.00}%</td>");
                    sb.AppendLine($"</tr>");
                }
                sb.AppendLine("</table>");
            }
            else
            {
                sb.AppendLine("<p>No open positions</p>");
            }

            // Trade Activity
            sb.AppendLine("<h3>📊 Trade Activity</h3>");
            sb.AppendLine("<table style='border-collapse: collapse; width: 100%;'>");
            sb.AppendLine("<tr style='background-color: #f5f5f5;'><th style='padding: 8px; text-align: left;'>Period</th><th style='text-align: right; padding: 8px;'>Trades</th><th style='text-align: right; padding: 8px;'>Win Rate</th><th style='text-align: right; padding: 8px;'>Avg Gain</th><th style='text-align: right; padding: 8px;'>Avg Loss</th></tr>");
            sb.AppendLine($"<tr><td style='padding: 8px;'>Today</td><td style='text-align: right; padding: 8px;'>{todayStats.TotalTrades}</td><td style='text-align: right; padding: 8px;'>{todayStats.WinRatePercent:0.0}%</td><td style='text-align: right; padding: 8px;'>${todayStats.AverageGain:N2}</td><td style='text-align: right; padding: 8px;'>${todayStats.AverageLoss:N2}</td></tr>");
            sb.AppendLine($"<tr><td style='padding: 8px;'>This Week</td><td style='text-align: right; padding: 8px;'>{weekStats.TotalTrades}</td><td style='text-align: right; padding: 8px;'>{weekStats.WinRatePercent:0.0}%</td><td style='text-align: right; padding: 8px;'>${weekStats.AverageGain:N2}</td><td style='text-align: right; padding: 8px;'>${weekStats.AverageLoss:N2}</td></tr>");
            sb.AppendLine($"<tr><td style='padding: 8px;'>This Month</td><td style='text-align: right; padding: 8px;'>{monthStats.TotalTrades}</td><td style='text-align: right; padding: 8px;'>{monthStats.WinRatePercent:0.0}%</td><td style='text-align: right; padding: 8px;'>${monthStats.AverageGain:N2}</td><td style='text-align: right; padding: 8px;'>${monthStats.AverageLoss:N2}</td></tr>");
            sb.AppendLine("</table>");

            // Benchmark Comparison
            sb.AppendLine("<h3>📊 Benchmark Comparison (Since Inception)</h3>");
            sb.AppendLine("<table style='border-collapse: collapse; width: 100%;'>");
            var portfolioColor = benchmarks.PortfolioReturn >= 0 ? "green" : "red";
            var spyColor = benchmarks.SpyReturn >= 0 ? "green" : "red";
            var qqqColor = benchmarks.QqqReturn >= 0 ? "green" : "red";
            var alphaColor = benchmarks.AlphaVsSpy >= 0 ? "green" : "red";
            sb.AppendLine($"<tr><td>Portfolio:</td><td style='text-align: right; color: {portfolioColor};'><strong>{benchmarks.PortfolioReturn:+0.00;-0.00}%</strong></td></tr>");
            sb.AppendLine($"<tr><td>S&P 500 (SPY):</td><td style='text-align: right; color: {spyColor};'>{benchmarks.SpyReturn:+0.00;-0.00}%</td></tr>");
            sb.AppendLine($"<tr><td>NASDAQ-100 (QQQ):</td><td style='text-align: right; color: {qqqColor};'>{benchmarks.QqqReturn:+0.00;-0.00}%</td></tr>");
            sb.AppendLine($"<tr><td><strong>Alpha vs SPY:</strong></td><td style='text-align: right; color: {alphaColor};'><strong>{benchmarks.AlphaVsSpy:+0.00;-0.00}%</strong></td></tr>");
            sb.AppendLine("</table>");

            // Footer
            sb.AppendLine("<hr/>");
            sb.AppendLine($"<p style='font-size: 12px; color: #666;'>Generated at {today:yyyy-MM-dd HH:mm:ss} UTC</p>");
            sb.AppendLine("<p style='font-size: 12px; color: #666;'>Azure Autonomous Trading System - For Educational Purposes Only</p>");

            return sb.ToString();
        }
    }
}