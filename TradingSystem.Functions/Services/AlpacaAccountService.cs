using Alpaca.Markets;
using Microsoft.Extensions.Logging;
using TradingSystem.Functions.Config;
using TradingSystem.Functions.Models;
using TradingSystem.Functions.Services.Interfaces;

namespace TradingSystem.Functions.Services
{
    /// <summary>
    /// Service for interacting with Alpaca Markets brokerage API
    /// </summary>
    public class AlpacaAccountService : IAlpacaAccountService
    {
        private readonly IAlpacaTradingClient _tradingClient;
        private readonly ILogger<AlpacaAccountService> _logger;

        public AlpacaAccountService(AlpacaConfig config, ILogger<AlpacaAccountService> logger)
        {
            _logger = logger;

            var isPaper = config.BaseUrl?.Contains("paper") ?? true;
            var environment = isPaper ? Environments.Paper : Environments.Live;

            var secretKey = new SecretKey(config.ApiKey, config.SecretKey);
            _tradingClient = environment.GetAlpacaTradingClient(secretKey);

            _logger.LogInformation("Alpaca client initialized for {env} trading", isPaper ? "PAPER" : "LIVE");
        }

        public async Task<AccountInfo> GetAccountInfoAsync()
        {
            try
            {
                var account = await _tradingClient.GetAccountAsync();

                return new AccountInfo
                {
                    Equity = account.Equity ?? 0,
                    Cash = account.TradableCash,
                    BuyingPower = account.BuyingPower ?? 0,
                    Status = account.Status.ToString(),
                    AccountNumber = account.AccountNumber ?? string.Empty,
                    RetrievedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get account info from Alpaca");
                throw;
            }
        }

        public async Task<List<PositionInfo>> GetPositionsAsync()
        {
            try
            {
                var positions = await _tradingClient.ListPositionsAsync();

                return positions.Select(p => new PositionInfo
                {
                    Symbol = p.Symbol,
                    Quantity = p.Quantity,  // Keep as decimal
                    AverageCostBasis = p.AverageEntryPrice,
                    CurrentPrice = p.AssetCurrentPrice ?? 0,
                    MarketValue = p.MarketValue ?? 0,
                    UnrealizedPL = p.UnrealizedProfitLoss ?? 0,
                    UnrealizedPLPercent = p.UnrealizedProfitLossPercent ?? 0,
                    Side = p.Side.ToString(),
                    AssetId = p.AssetId.ToString()
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get positions from Alpaca");
                throw;
            }
        }

        /// <summary>
        /// Cancels all open orders. Returns true if successful.
        /// </summary>
        public async Task<bool> CancelAllOrdersAsync()
        {
            try
            {
                var cancelledOrders = await _tradingClient.CancelAllOrdersAsync();
                var count = cancelledOrders.Count();
                _logger.LogInformation("Cancelled {count} orders", count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel all orders");
                return false;
            }
        }

        /// <summary>
        /// Gets account activities between dates. Returns List of object for flexibility.
        /// </summary>
        public async Task<List<object>> GetAccountActivitiesAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                // Use simple request - SDK version compatibility
                var activities = await _tradingClient.ListAccountActivitiesAsync(
                    new AccountActivitiesRequest());

                // Filter by date in memory and return as objects
                var result = activities
                    .Where(a => a.ActivityDateTimeUtc >= startDate && a.ActivityDateTimeUtc <= endDate)
                    .Select(a => (object)new
                    {
                        ActivityType = a.ActivityType.ToString(),
                        ActivityDate = a.ActivityDateTimeUtc,
                        Id = a.ActivityId
                    })
                    .ToList();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get account activities");
                return new List<object>();
            }
        }
    }
}