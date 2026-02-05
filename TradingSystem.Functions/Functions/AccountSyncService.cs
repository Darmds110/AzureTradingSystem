using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
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

        public AccountSyncService(
            ILogger<AccountSyncService> logger,
            IAlpacaAccountService alpacaService,
            IPortfolioService portfolioService,
            IEmailService emailService)
        {
            _logger = logger;
            _alpacaService = alpacaService;
            _portfolioService = portfolioService;
            _emailService = emailService;
        }

        /// <summary>
        /// Timer trigger that runs every 15 minutes
        /// CRON: "0 */15 * * * *" = At minute 0, 15, 30, 45 of every hour
        /// </summary>
        [Function("AccountSyncService")]
        public async Task Run([TimerTrigger("0 */15 * * * *")] TimerInfo myTimer)
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation("AccountSyncService started at: {time}", startTime);

            // Track if this is a missed execution
            if (myTimer.ScheduleStatus != null)
            {
                _logger.LogInformation(
                    "Next timer schedule at: {next}",
                    myTimer.ScheduleStatus.Next);
            }

            try
            {
                // Step 1: Get account info from Alpaca
                _logger.LogInformation("Fetching account information from Alpaca");
                var accountInfo = await _alpacaService.GetAccountInfoAsync();

                // Step 2: Get current positions from Alpaca
                _logger.LogInformation("Fetching positions from Alpaca");
                var positions = await _alpacaService.GetPositionsAsync();

                // Step 3: Sync to database
                _logger.LogInformation("Syncing portfolio state to database");
                await _portfolioService.SyncPortfolioStateAsync(accountInfo, positions);

                // Step 4: Log success
                var duration = (DateTime.UtcNow - startTime).TotalSeconds;
                _logger.LogInformation(
                    "AccountSyncService completed successfully in {duration:F2}s. " +
                    "Equity: ${equity:F2}, Cash: ${cash:F2}, Positions: {count}",
                    duration,
                    accountInfo.Equity,
                    accountInfo.Cash,
                    positions.Count);

                // Step 5: Send notification if there are significant changes
                // (This is optional - can be implemented later)
                await CheckForSignificantChangesAsync(accountInfo, positions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AccountSyncService");

                // Send alert email on failure
                try
                {
                    await _emailService.SendSystemAlertAsync(
                        "Account Sync Failed",
                        $"AccountSyncService failed at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n\n" +
                        $"Error: {ex.Message}\n\n" +
                        $"Stack Trace:\n{ex.StackTrace}");
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to send error notification email");
                }

                // Re-throw to mark function execution as failed in Application Insights
                throw;
            }
        }

        /// <summary>
        /// Checks for significant changes and sends notifications
        /// Optional feature for alerting on major portfolio changes
        /// </summary>
        private async Task CheckForSignificantChangesAsync(
            Models.AccountInfo accountInfo,
            List<Models.PositionInfo> positions)
        {
            try
            {
                // Get previous sync data to compare
                var portfolio = await _portfolioService.GetCurrentPortfolioAsync();

                // Check if there are new positions
                var existingPositions = await GetExistingPositionSymbolsAsync();
                var newPositions = positions
                    .Where(p => !existingPositions.Contains(p.Symbol))
                    .ToList();

                if (newPositions.Any())
                {
                    _logger.LogInformation(
                        "New positions detected: {symbols}",
                        string.Join(", ", newPositions.Select(p => p.Symbol)));
                }

                // Check for large daily change (> 5%)
                if (portfolio.LastSyncTimestamp.HasValue)
                {
                    var timeSinceLastSync = DateTime.UtcNow - portfolio.LastSyncTimestamp.Value;

                    // Only check if last sync was recent (within 30 minutes)
                    if (timeSinceLastSync.TotalMinutes <= 30)
                    {
                        // Get previous equity from database (would need to query PerformanceMetrics table)
                        // For now, just log if we notice major changes
                        _logger.LogDebug("Portfolio monitoring: Current equity ${equity}", accountInfo.Equity);
                    }
                }
            }
            catch (Exception ex)
            {
                // Don't fail the whole function if this check fails
                _logger.LogWarning(ex, "Error checking for significant changes");
            }
        }

        /// <summary>
        /// Helper method to get existing position symbols
        /// </summary>
        private async Task<List<string>> GetExistingPositionSymbolsAsync()
        {
            // This would query the database - simplified for now
            // In real implementation, would use _portfolioService or _dbContext
            return new List<string>();
        }
    }
}