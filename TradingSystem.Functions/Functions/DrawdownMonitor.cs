using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using TradingSystem.Functions.Models;
using TradingSystem.Functions.Services.Interfaces;

namespace TradingSystem.Functions.Functions
{
    /// <summary>
    /// Azure Function that monitors portfolio drawdown in real-time
    /// Runs every 5 minutes during market hours to catch rapid drawdowns
    /// Works in conjunction with AccountSyncService for redundant protection
    /// </summary>
    public class DrawdownMonitor
    {
        private readonly ILogger<DrawdownMonitor> _logger;
        private readonly IAlpacaAccountService _alpacaService;
        private readonly IPortfolioService _portfolioService;
        private readonly IEmailService _emailService;
        private readonly ITableStorageService _tableStorageService;

        // Risk thresholds (same as AccountSyncService for consistency)
        private const decimal DRAWDOWN_WARNING_THRESHOLD = -15.0m;
        private const decimal DRAWDOWN_HALT_THRESHOLD = -20.0m;
        private const decimal DAILY_LOSS_WARNING_THRESHOLD = -4.0m;  // 4% daily loss warning
        private const decimal DAILY_LOSS_HALT_THRESHOLD = -5.0m;    // 5% daily loss halt

        public DrawdownMonitor(
            ILogger<DrawdownMonitor> logger,
            IAlpacaAccountService alpacaService,
            IPortfolioService portfolioService,
            IEmailService emailService,
            ITableStorageService tableStorageService)
        {
            _logger = logger;
            _alpacaService = alpacaService;
            _portfolioService = portfolioService;
            _emailService = emailService;
            _tableStorageService = tableStorageService;
        }

        /// <summary>
        /// Timer trigger that runs every 5 minutes
        /// CRON: "0 */5 * * * *" = Every 5 minutes
        /// </summary>
        [Function("DrawdownMonitor")]
        public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation("DrawdownMonitor triggered at: {time}", DateTime.UtcNow);

            try
            {
                // Check if market is open
                var isMarketOpen = await _tableStorageService.IsMarketOpenAsync();
                if (!isMarketOpen)
                {
                    _logger.LogDebug("Market is closed. Skipping drawdown monitoring.");
                    return;
                }

                // Get current portfolio
                var portfolio = await _portfolioService.GetCurrentPortfolioAsync();
                if (portfolio == null)
                {
                    _logger.LogError("No portfolio found. Skipping drawdown monitoring.");
                    return;
                }

                // If trading is already halted, just log and return
                if (portfolio.IsTradingPaused)
                {
                    _logger.LogInformation("Trading already halted. Monitoring for recovery only.");
                    await MonitorRecoveryAsync(portfolio);
                    return;
                }

                // Get real-time account info from Alpaca
                var accountInfo = await _alpacaService.GetAccountInfoAsync();
                var currentEquity = accountInfo.Equity;

                // Calculate real-time drawdown from peak
                decimal drawdownFromPeak = 0;
                if (portfolio.PeakValue > 0)
                {
                    drawdownFromPeak = ((currentEquity - portfolio.PeakValue) / portfolio.PeakValue) * 100;
                }

                // Calculate daily loss (from day's opening value)
                var dailyOpenValue = await GetDailyOpenValueAsync(portfolio.PortfolioId);
                decimal dailyLoss = 0;
                if (dailyOpenValue > 0)
                {
                    dailyLoss = ((currentEquity - dailyOpenValue) / dailyOpenValue) * 100;
                }

                _logger.LogInformation(
                    "Real-time check: Equity=${equity:N2}, Drawdown={drawdown:F2}%, Daily Loss={dailyLoss:F2}%",
                    currentEquity, drawdownFromPeak, dailyLoss);

                // Check drawdown from peak (20% halt threshold)
                if (drawdownFromPeak <= DRAWDOWN_HALT_THRESHOLD)
                {
                    await HaltTradingForDrawdownAsync(portfolio, currentEquity, drawdownFromPeak, "peak drawdown");
                    return;
                }

                // Check daily loss (5% halt threshold)
                if (dailyLoss <= DAILY_LOSS_HALT_THRESHOLD)
                {
                    await HaltTradingForDrawdownAsync(portfolio, currentEquity, dailyLoss, "daily loss");
                    return;
                }

                // Check for warning thresholds
                await CheckWarningThresholdsAsync(portfolio, currentEquity, drawdownFromPeak, dailyLoss);

                // Update drawdown in portfolio (for dashboard/reporting)
                await _portfolioService.UpdateDrawdownAsync(drawdownFromPeak);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DrawdownMonitor");
                // Don't throw - we want monitoring to continue even if one check fails
            }
        }

        /// <summary>
        /// Halts trading due to excessive drawdown
        /// </summary>
        private async Task HaltTradingForDrawdownAsync(
            Portfolio portfolio,
            decimal currentEquity,
            decimal lossPercent,
            string lossType)
        {
            _logger.LogCritical(
                "CRITICAL: {lossType} of {loss:F2}% detected. HALTING TRADING IMMEDIATELY.",
                lossType, lossPercent);

            var reason = $"{lossType} of {lossPercent:F2}% exceeded threshold";
            await _portfolioService.HaltTradingAsync(reason);

            // Send critical alert
            await _emailService.SendAlertAsync(
                $"🚨 TRADING HALTED - {lossType.ToUpper()} EXCEEDED",
                $"Trading has been IMMEDIATELY HALTED due to {lossType}.\n\n" +
                $"Loss: {lossPercent:F2}%\n" +
                $"Current Equity: ${currentEquity:N2}\n" +
                $"Peak Value: ${portfolio.PeakValue:N2}\n" +
                $"Initial Capital: ${portfolio.InitialCapital:N2}\n\n" +
                $"This is an automated safety measure. MANUAL REVIEW REQUIRED before resuming trading.\n\n" +
                $"Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
                "CRITICAL");
        }

        /// <summary>
        /// Checks warning thresholds and sends alerts if needed
        /// </summary>
        private async Task CheckWarningThresholdsAsync(
            Portfolio portfolio,
            decimal currentEquity,
            decimal drawdownFromPeak,
            decimal dailyLoss)
        {
            var today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

            // Check peak drawdown warning (15%)
            if (drawdownFromPeak <= DRAWDOWN_WARNING_THRESHOLD)
            {
                var warningKey = $"DrawdownWarning_{today}";
                var alreadyWarned = await _tableStorageService.GetCacheValueAsync<bool>(warningKey);

                if (!alreadyWarned)
                {
                    _logger.LogWarning("Drawdown warning: {drawdown:F2}% approaching halt threshold", drawdownFromPeak);

                    await _emailService.SendAlertAsync(
                        "⚠️ Drawdown Warning",
                        $"Portfolio drawdown is approaching the halt threshold.\n\n" +
                        $"Current Drawdown: {drawdownFromPeak:F2}%\n" +
                        $"Halt Threshold: {DRAWDOWN_HALT_THRESHOLD}%\n" +
                        $"Current Equity: ${currentEquity:N2}\n" +
                        $"Peak Value: ${portfolio.PeakValue:N2}\n\n" +
                        $"Monitoring will continue. Trading will halt automatically at {DRAWDOWN_HALT_THRESHOLD}%.",
                        "HIGH");

                    await _tableStorageService.SetCacheValueAsync(warningKey, true, TimeSpan.FromHours(24));
                }
            }

            // Check daily loss warning (4%)
            if (dailyLoss <= DAILY_LOSS_WARNING_THRESHOLD && dailyLoss > DAILY_LOSS_HALT_THRESHOLD)
            {
                var warningKey = $"DailyLossWarning_{today}";
                var alreadyWarned = await _tableStorageService.GetCacheValueAsync<bool>(warningKey);

                if (!alreadyWarned)
                {
                    _logger.LogWarning("Daily loss warning: {loss:F2}% approaching limit", dailyLoss);

                    await _emailService.SendAlertAsync(
                        "⚠️ Daily Loss Warning",
                        $"Daily loss is approaching the halt threshold.\n\n" +
                        $"Today's Loss: {dailyLoss:F2}%\n" +
                        $"Halt Threshold: {DAILY_LOSS_HALT_THRESHOLD}%\n" +
                        $"Current Equity: ${currentEquity:N2}\n\n" +
                        $"Trading will halt automatically at {DAILY_LOSS_HALT_THRESHOLD}% daily loss.",
                        "HIGH");

                    await _tableStorageService.SetCacheValueAsync(warningKey, true, TimeSpan.FromHours(24));
                }
            }
        }

        /// <summary>
        /// Gets the portfolio value at market open today
        /// </summary>
        private async Task<decimal> GetDailyOpenValueAsync(int portfolioId)
        {
            var cacheKey = $"DailyOpenValue_{portfolioId}_{DateTime.UtcNow:yyyy-MM-dd}";
            var cachedValue = await _tableStorageService.GetCacheValueAsync<decimal>(cacheKey);

            if (cachedValue > 0)
            {
                return cachedValue;
            }

            // If not cached, get from Alpaca (first call of the day)
            var accountInfo = await _alpacaService.GetAccountInfoAsync();
            var openValue = accountInfo.Equity;

            // Cache it for the rest of the day
            await _tableStorageService.SetCacheValueAsync(cacheKey, openValue, TimeSpan.FromHours(18));

            _logger.LogInformation("Cached daily open value: ${value:N2}", openValue);
            return openValue;
        }

        /// <summary>
        /// Monitors for recovery when trading is halted
        /// </summary>
        private async Task MonitorRecoveryAsync(Portfolio portfolio)
        {
            var accountInfo = await _alpacaService.GetAccountInfoAsync();
            var currentEquity = accountInfo.Equity;

            decimal drawdownFromPeak = 0;
            if (portfolio.PeakValue > 0)
            {
                drawdownFromPeak = ((currentEquity - portfolio.PeakValue) / portfolio.PeakValue) * 100;
            }

            _logger.LogInformation(
                "Recovery monitoring: Equity=${equity:N2}, Drawdown={drawdown:F2}% (Halted at {halt}%)",
                currentEquity, drawdownFromPeak, DRAWDOWN_HALT_THRESHOLD);

            // If recovered above warning threshold, notify
            if (drawdownFromPeak > DRAWDOWN_WARNING_THRESHOLD)
            {
                var recoveryKey = $"RecoveryNotified_{DateTime.UtcNow:yyyy-MM-dd}";
                var alreadyNotified = await _tableStorageService.GetCacheValueAsync<bool>(recoveryKey);

                if (!alreadyNotified)
                {
                    await _emailService.SendAlertAsync(
                        "📈 Portfolio Recovery Detected",
                        $"The portfolio has recovered above the warning threshold.\n\n" +
                        $"Current Drawdown: {drawdownFromPeak:F2}%\n" +
                        $"Warning Threshold: {DRAWDOWN_WARNING_THRESHOLD}%\n" +
                        $"Current Equity: ${currentEquity:N2}\n\n" +
                        $"Trading is still HALTED. Manual review and resume required.",
                        "MEDIUM");

                    await _tableStorageService.SetCacheValueAsync(recoveryKey, true, TimeSpan.FromHours(24));
                }
            }
        }
    }
}