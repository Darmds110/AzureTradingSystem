using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using TradingSystem.Functions.Models;
using TradingSystem.Functions.Services.Interfaces;

namespace TradingSystem.Functions.Functions
{
    /// <summary>
    /// Azure Function that syncs portfolio state with Alpaca account
    /// Runs every 15 minutes during market hours
    /// </summary>
    public class AccountSyncService
    {
        private readonly ILogger<AccountSyncService> _logger;
        private readonly IAlpacaAccountService _alpacaService;
        private readonly IPortfolioService _portfolioService;
        private readonly IEmailService _emailService;
        private readonly ITableStorageService _tableStorageService;

        // Risk thresholds
        private const decimal DRAWDOWN_WARNING_THRESHOLD = -15.0m;  // 15% drawdown - early warning
        private const decimal DRAWDOWN_HALT_THRESHOLD = -20.0m;    // 20% drawdown - halt trading

        public AccountSyncService(
            ILogger<AccountSyncService> logger,
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
        /// Timer trigger that runs every 15 minutes
        /// </summary>
        [Function("AccountSyncService")]
        public async Task Run([TimerTrigger("0 */15 * * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation("AccountSyncService triggered at: {time}", DateTime.UtcNow);

            try
            {
                var isMarketOpen = await _tableStorageService.IsMarketOpenAsync();
                _logger.LogInformation("Market open status: {status}", isMarketOpen);

                var portfolio = await _portfolioService.GetCurrentPortfolioAsync();
                if (portfolio == null)
                {
                    _logger.LogError("No portfolio found in database. Skipping sync.");
                    return;
                }

                if (portfolio.IsTradingPaused)
                {
                    _logger.LogWarning("Trading is currently HALTED. Reason: {reason}. Still syncing for monitoring.",
                        portfolio.PausedReason);
                }

                _logger.LogInformation("Fetching account information from Alpaca...");
                var accountInfo = await _alpacaService.GetAccountInfoAsync();

                _logger.LogInformation("Fetching positions from Alpaca...");
                var positions = await _alpacaService.GetPositionsAsync();

                _logger.LogInformation(
                    "Alpaca data retrieved: Equity=${equity}, Cash=${cash}, Positions={posCount}",
                    accountInfo.Equity,
                    accountInfo.Cash,
                    positions.Count);

                // Use 2-parameter version
                await _portfolioService.SyncPortfolioStateAsync(accountInfo, positions);

                // Re-fetch portfolio to get updated values
                portfolio = await _portfolioService.GetCurrentPortfolioAsync();

                if (portfolio != null)
                {
                    // Check drawdown and handle risk management
                    await CheckDrawdownAndManageRiskAsync(portfolio, accountInfo);

                    // Update holding periods for positions
                    await _portfolioService.UpdateHoldingPeriodsAsync();

                    _logger.LogInformation(
                        "Account sync complete. Equity: ${equity}, Drawdown: {drawdown:F2}%, Trading Halted: {halted}",
                        portfolio.CurrentEquity,
                        portfolio.CurrentDrawdownPercent,
                        portfolio.IsTradingPaused);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AccountSyncService");

                try
                {
                    await _emailService.SendAlertAsync(
                        "Account Sync Failed",
                        $"The AccountSyncService encountered an error:\n\n{ex.Message}\n\nPlease check the logs for details.",
                        "HIGH");
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to send error alert email");
                }

                throw;
            }
        }

        private async Task CheckDrawdownAndManageRiskAsync(Portfolio portfolio, AccountInfo accountInfo)
        {
            var drawdown = portfolio.CurrentDrawdownPercent;

            _logger.LogInformation("Checking drawdown: {drawdown:F2}% (Warning: {warn}%, Halt: {halt}%)",
                drawdown, DRAWDOWN_WARNING_THRESHOLD, DRAWDOWN_HALT_THRESHOLD);

            // Check for 20% drawdown - HALT TRADING
            if (drawdown <= DRAWDOWN_HALT_THRESHOLD && !portfolio.IsTradingPaused)
            {
                _logger.LogCritical(
                    "CRITICAL: Drawdown of {drawdown:F2}% exceeds {threshold}% threshold. HALTING TRADING.",
                    drawdown, DRAWDOWN_HALT_THRESHOLD);

                // Use 1-parameter version
                await _portfolioService.HaltTradingAsync(
                    $"Drawdown of {drawdown:F2}% exceeded {DRAWDOWN_HALT_THRESHOLD}% threshold");

                await _emailService.SendAlertAsync(
                    "🚨 CRITICAL: Trading Halted - Drawdown Exceeded",
                    $"Trading has been automatically HALTED due to excessive drawdown.\n\n" +
                    $"Current Drawdown: {drawdown:F2}%\n" +
                    $"Threshold: {DRAWDOWN_HALT_THRESHOLD}%\n" +
                    $"Current Equity: ${accountInfo.Equity:N2}\n" +
                    $"Peak Value: ${portfolio.PeakValue:N2}\n" +
                    $"Loss from Peak: ${portfolio.PeakValue - accountInfo.Equity:N2}\n\n" +
                    $"MANUAL INTERVENTION REQUIRED to resume trading.\n" +
                    $"Please review positions and market conditions before resuming.",
                    "CRITICAL");

                return;
            }

            // Check for 15% drawdown - EARLY WARNING
            if (drawdown <= DRAWDOWN_WARNING_THRESHOLD && drawdown > DRAWDOWN_HALT_THRESHOLD)
            {
                _logger.LogWarning(
                    "WARNING: Drawdown of {drawdown:F2}% is approaching {threshold}% halt threshold.",
                    drawdown, DRAWDOWN_HALT_THRESHOLD);

                var lastWarningKey = $"DrawdownWarning_{DateTime.UtcNow:yyyy-MM-dd}";
                var alreadyWarned = await _tableStorageService.GetCacheValueAsync<bool>(lastWarningKey);

                if (!alreadyWarned)
                {
                    await _emailService.SendAlertAsync(
                        "⚠️ WARNING: Drawdown Approaching Limit",
                        $"Portfolio drawdown is approaching the halt threshold.\n\n" +
                        $"Current Drawdown: {drawdown:F2}%\n" +
                        $"Warning Threshold: {DRAWDOWN_WARNING_THRESHOLD}%\n" +
                        $"Halt Threshold: {DRAWDOWN_HALT_THRESHOLD}%\n" +
                        $"Current Equity: ${accountInfo.Equity:N2}\n" +
                        $"Peak Value: ${portfolio.PeakValue:N2}\n\n" +
                        $"Trading will be automatically halted if drawdown reaches {DRAWDOWN_HALT_THRESHOLD}%.",
                        "HIGH");

                    await _tableStorageService.SetCacheValueAsync(lastWarningKey, true, TimeSpan.FromHours(24));
                }
            }

            if (drawdown > DRAWDOWN_WARNING_THRESHOLD && portfolio.CurrentDrawdownPercent <= DRAWDOWN_WARNING_THRESHOLD)
            {
                _logger.LogInformation("Portfolio has recovered above warning threshold. Current drawdown: {drawdown:F2}%", drawdown);
            }
        }
    }
}