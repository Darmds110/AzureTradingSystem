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
                    AccountNumber = account.AccountNumber,
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
                    Quantity = (int)p.Quantity,
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

        public async Task<int> CancelAllOrdersAsync()
        {
            try
            {
                var cancelledOrders = await _tradingClient.CancelAllOrdersAsync();
                var count = cancelledOrders.Count();
                _logger.LogInformation("Cancelled {count} orders", count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel all orders");
                throw;
            }
        }

        public async Task<List<AccountActivity>> GetAccountActivitiesAsync(DateTime? after = null, DateTime? until = null)
        {
            try
            {
                // Use simple request - SDK version may not support all parameters
                var activities = await _tradingClient.ListAccountActivitiesAsync(
                    new AccountActivitiesRequest());

                var result = activities.AsEnumerable();

                if (after.HasValue)
                {
                    result = result.Where(a => a.ActivityDateTime >= after.Value);
                }

                if (until.HasValue)
                {
                    result = result.Where(a => a.ActivityDateTime <= until.Value);
                }

                return result.Select(a => new AccountActivity
                {
                    ActivityType = a.ActivityType.ToString(),
                    ActivityDateTime = a.ActivityDateTime,
                    Symbol = (a as ITradeActivity)?.Symbol ?? string.Empty,
                    Quantity = (int)((a as ITradeActivity)?.Quantity ?? 0),
                    Price = (a as ITradeActivity)?.Price ?? 0,
                    Side = (a as ITradeActivity)?.Side.ToString() ?? string.Empty
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get account activities");
                return new List<AccountActivity>();
            }
        }
    }

    public class AccountActivity
    {
        public string ActivityType { get; set; } = string.Empty;
        public DateTime ActivityDateTime { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string Side { get; set; } = string.Empty;
    }
}