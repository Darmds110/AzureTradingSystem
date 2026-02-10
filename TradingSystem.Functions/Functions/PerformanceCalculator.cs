using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using TradingSystem.Functions.Services.Interfaces;

namespace TradingSystem.Functions.Functions
{
    /// <summary>
    /// Azure Function that calculates daily performance metrics
    /// Runs daily after market close (5:00 PM ET / 22:00 UTC)
    /// </summary>
    public class PerformanceCalculator
    {
        private readonly ILogger<PerformanceCalculator> _logger;
        private readonly IPerformanceService _performanceService;
        private readonly IPortfolioService _portfolioService;
        private readonly IEmailService _emailService;

        public PerformanceCalculator(
            ILogger<PerformanceCalculator> logger,
            IPerformanceService performanceService,
            IPortfolioService portfolioService,
            IEmailService emailService)
        {
            _logger = logger;
            _performanceService = performanceService;
            _portfolioService = portfolioService;
            _emailService = emailService;
        }

        /// <summary>
        /// Timer trigger that runs daily at 5:00 PM ET (22:00 UTC)
        /// </summary>
        [Function("PerformanceCalculator")]
        public async Task Run([TimerTrigger("0 0 22 * * *")] TimerInfo timerInfo)
        {
            _logger.LogInformation("PerformanceCalculator triggered at: {time}", DateTime.UtcNow);

            try
            {
                var portfolio = await _portfolioService.GetCurrentPortfolioAsync();
                if (portfolio == null)
                {
                    _logger.LogError("No portfolio found. Skipping performance calculation.");
                    return;
                }

                _logger.LogInformation("Calculating daily performance metrics for portfolio: {name}",
                    portfolio.PortfolioName);

                // Calculate and store daily metrics
                var dailyMetrics = await _performanceService.CalculateDailyMetricsAsync(portfolio.PortfolioId);

                _logger.LogInformation(
                    "Daily metrics calculated: Return={dailyReturn:F2}%, Total Return={totalReturn:F2}%",
                    dailyMetrics.PeriodReturnPercent,
                    dailyMetrics.TotalReturnPercent);

                // Calculate weekly metrics (on Fridays)
                if (DateTime.UtcNow.DayOfWeek == DayOfWeek.Friday)
                {
                    _logger.LogInformation("Friday detected - calculating weekly metrics");
                    var weeklyMetrics = await _performanceService.CalculateWeeklyMetricsAsync(portfolio.PortfolioId);

                    _logger.LogInformation(
                        "Weekly metrics calculated: Return={weeklyReturn:F2}%, Win Rate={winRate:F1}%",
                        weeklyMetrics.PeriodReturnPercent,
                        weeklyMetrics.WinRate);

                    await _emailService.SendWeeklySummaryAsync(
                        weeklyMetrics.PortfolioValue,
                        weeklyMetrics.PeriodReturnPercent,
                        weeklyMetrics.TotalReturnPercent,
                        weeklyMetrics.TotalTrades ?? 0,
                        weeklyMetrics.WinRate ?? 0);
                }

                // Calculate monthly metrics (on last day of month)
                if (DateTime.UtcNow.Day == DateTime.DaysInMonth(DateTime.UtcNow.Year, DateTime.UtcNow.Month))
                {
                    _logger.LogInformation("End of month detected - calculating monthly metrics");
                    var monthlyMetrics = await _performanceService.CalculateMonthlyMetricsAsync(portfolio.PortfolioId);

                    _logger.LogInformation(
                        "Monthly metrics calculated: Return={monthlyReturn:F2}%, Sharpe={sharpe:F2}",
                        monthlyMetrics.PeriodReturnPercent,
                        monthlyMetrics.SharpeRatio);

                    await _emailService.SendMonthlySummaryAsync(
                        monthlyMetrics.PortfolioValue,
                        monthlyMetrics.PeriodReturnPercent,
                        monthlyMetrics.TotalReturnPercent,
                        monthlyMetrics.SharpeRatio ?? 0,
                        monthlyMetrics.MaxDrawdownPercent,
                        monthlyMetrics.TotalTrades ?? 0,
                        0); // Azure costs placeholder
                }

                // Compare against benchmarks
                var benchmarkComparison = await _performanceService.CompareToBenchmarksAsync(
                    portfolio.PortfolioId,
                    DateTime.UtcNow.Date);

                _logger.LogInformation(
                    "Benchmark comparison: Portfolio={portfolio:F2}%, SPY={spy:F2}%, QQQ={qqq:F2}%",
                    benchmarkComparison.PortfolioReturn,
                    benchmarkComparison.SpyReturn,
                    benchmarkComparison.QqqReturn);

                await _performanceService.CalculateStrategyPerformanceAsync(portfolio.PortfolioId);

                _logger.LogInformation("Performance calculation complete");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating performance metrics");

                try
                {
                    await _emailService.SendAlertAsync(
                        "Performance Calculator Failed",
                        $"The PerformanceCalculator encountered an error:\n\n{ex.Message}",
                        "HIGH");
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to send error alert email");
                }

                throw;
            }
        }
    }
}