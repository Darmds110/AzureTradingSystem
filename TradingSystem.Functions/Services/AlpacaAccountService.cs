using Alpaca.Markets;
using Microsoft.Extensions.Logging;
using TradingSystem.Functions.Models;
using TradingSystem.Functions.Services.Interfaces;

namespace TradingSystem.Functions.Services
{
    /// <summary>
    /// Implementation of Alpaca account service using Alpaca.Markets SDK
    /// </summary>
    public class AlpacaAccountService : IAlpacaAccountService
    {
        private readonly IAlpacaTradingClient _tradingClient;
        private readonly ILogger<AlpacaAccountService> _logger;

        public AlpacaAccountService(ILogger<AlpacaAccountService> logger)
        {
            _logger = logger;

            // Get credentials from environment variables (set in Azure Function App Settings)
            var apiKey = Environment.GetEnvironmentVariable("AlpacaApiKey")
                ?? throw new InvalidOperationException("AlpacaApiKey not configured");
            var secretKey = Environment.GetEnvironmentVariable("AlpacaSecretKey")
                ?? throw new InvalidOperationException("AlpacaSecretKey not configured");
            var baseUrl = Environment.GetEnvironmentVariable("AlpacaBaseUrl")
                ?? "https://paper-api.alpaca.markets";

            _logger.LogInformation("Initializing Alpaca client with base URL: {baseUrl}", baseUrl);

            // Create Alpaca trading client
            var secretKeyPair = new SecretKey(apiKey, secretKey);

            // Use paper trading or live based on base URL
            if (baseUrl.Contains("paper"))
            {
                _tradingClient = Environments.Paper.GetAlpacaTradingClient(secretKeyPair);
                _logger.LogInformation("Using Alpaca PAPER trading environment");
            }
            else
            {
                _tradingClient = Environments.Live.GetAlpacaTradingClient(secretKeyPair);
                _logger.LogWarning("Using Alpaca LIVE trading environment");
            }
        }

        /// <summary>
        /// Gets current account information from Alpaca
        /// </summary>
        public async Task<AccountInfo> GetAccountInfoAsync()
        {
            try
            {
                _logger.LogInformation("Fetching account information from Alpaca");

                var account = await _tradingClient.GetAccountAsync();

                var accountInfo = new AccountInfo
                {
                    Equity = account.Equity,
                    Cash = account.TradableCash,
                    BuyingPower = account.BuyingPower,
                    Status = account.Status.ToString(),
                    AccountNumber = account.AccountNumber,
                    RetrievedAt = DateTime.UtcNow
                };

                _logger.LogInformation(
                    "Account info retrieved: Equity=${equity}, Cash=${cash}, Status={status}",
                    accountInfo.Equity,
                    accountInfo.Cash,
                    accountInfo.Status);

                return accountInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching account information from Alpaca");
                throw;
            }
        }

        /// <summary>
        /// Gets all current positions from Alpaca
        /// </summary>
        public async Task<List<PositionInfo>> GetPositionsAsync()
        {
            try
            {
                _logger.LogInformation("Fetching positions from Alpaca");

                var positions = await _tradingClient.ListPositionsAsync();

                var positionList = positions.Select(p => new PositionInfo
                {
                    Symbol = p.Symbol,
                    Quantity = p.Quantity,
                    AverageCostBasis = p.AverageEntryPrice,
                    CurrentPrice = p.AssetCurrentPrice ?? p.AverageEntryPrice,
                    MarketValue = p.MarketValue ?? (p.Quantity * p.AverageEntryPrice),
                    UnrealizedPL = p.UnrealizedProfitLoss ?? 0,
                    UnrealizedPLPercent = p.UnrealizedProfitLossPercent ?? 0,
                    Side = p.Side.ToString().ToLower(),
                    AssetId = p.AssetId.ToString()
                }).ToList();

                _logger.LogInformation("Retrieved {count} positions from Alpaca", positionList.Count);

                return positionList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching positions from Alpaca");
                throw;
            }
        }

        /// <summary>
        /// Cancels all pending orders in the account
        /// Used when trading halt is triggered
        /// </summary>
        public async Task<bool> CancelAllOrdersAsync()
        {
            try
            {
                _logger.LogWarning("Cancelling all pending orders");

                var orders = await _tradingClient.ListOrdersAsync(
                    new ListOrdersRequest { OrderStatusFilter = OrderStatusFilter.Open });

                foreach (var order in orders)
                {
                    try
                    {
                        await _tradingClient.CancelOrderAsync(order.OrderId);
                        _logger.LogInformation("Cancelled order {orderId} for {symbol}",
                            order.OrderId, order.Symbol);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error cancelling order {orderId}", order.OrderId);
                    }
                }

                _logger.LogInformation("Cancelled {count} orders", orders.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling all orders");
                return false;
            }
        }

        /// <summary>
        /// Gets account activities for specified date range
        /// </summary>
        public async Task<List<object>> GetAccountActivitiesAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogInformation("Fetching account activities from {start} to {end}",
                    startDate, endDate);

                var request = new AccountActivitiesRequest(AccountActivityType.All)
                {
                    After = startDate,
                    Until = endDate
                };

                var activities = await _tradingClient.ListAccountActivitiesAsync(request);

                _logger.LogInformation("Retrieved {count} activities", activities.Count);

                // Convert to list of objects for now
                // Can be expanded to specific activity types later
                return activities.Cast<object>().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching account activities");
                throw;
            }
        }
    }
}