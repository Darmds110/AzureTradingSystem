using Alpaca.Markets;
using Microsoft.Extensions.Logging;
using TradingSystem.Functions.Config;
using TradingSystem.Functions.Services.Interfaces;

namespace TradingSystem.Functions.Services;

public class MarketDataService : IMarketDataService
{
    private readonly IAlpacaTradingClient _alpacaTradingClient;
    private readonly IAlpacaDataClient _alpacaDataClient;
    private readonly ITableStorageService _tableStorage;
    private readonly ILogger<MarketDataService> _logger;

    public MarketDataService(
        AlpacaConfig config,
        ITableStorageService tableStorage,
        ILogger<MarketDataService> logger)
    {
        _tableStorage = tableStorage;
        _logger = logger;

        var secretKey = new SecretKey(config.ApiKey, config.SecretKey);

        // Trading client for clock/calendar
        _alpacaTradingClient = Environments.Paper.GetAlpacaTradingClient(secretKey);

        // Data client for quotes and historical data
        _alpacaDataClient = Environments.Paper.GetAlpacaDataClient(secretKey);
    }

    public async Task<bool> IsMarketOpen()
    {
        try
        {
            var clock = await _alpacaTradingClient.GetClockAsync();
            return clock.IsOpen;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if market is open");

            var today = DateTime.UtcNow.Date;
            var schedule = await _tableStorage.GetMarketScheduleAsync(today);
            return schedule.isOpen;
        }
    }

    public async Task<DateTime?> GetNextMarketOpen()
    {
        try
        {
            var clock = await _alpacaTradingClient.GetClockAsync();
            return clock.NextOpenUtc;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting next market open time");
            return null;
        }
    }

    public async Task<DateTime?> GetNextMarketClose()
    {
        try
        {
            var clock = await _alpacaTradingClient.GetClockAsync();
            return clock.NextCloseUtc;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting next market close time");
            return null;
        }
    }

    public async Task UpdateMarketSchedule()
    {
        try
        {
            var clock = await _alpacaTradingClient.GetClockAsync();
            var today = DateTime.UtcNow.Date;

            // Use Async version
            await _tableStorage.SaveMarketScheduleAsync(
                today,
                clock.IsOpen,
                clock.NextOpenUtc,
                clock.NextCloseUtc
            );

            _logger.LogInformation(
                "Cached market schedule - IsOpen: {IsOpen}, NextOpen: {NextOpen}, NextClose: {NextClose}",
                clock.IsOpen,
                clock.NextOpenUtc,
                clock.NextCloseUtc
            );

            _logger.LogInformation("Market schedule update completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating market schedule");
            throw;
        }
    }

    public async Task<StockQuote?> GetLatestQuote(string symbol)
    {
        try
        {
            // Use the new LatestMarketDataRequest for stocks
            var latestRequest = new LatestMarketDataRequest(symbol);

            // Get latest trade - this works with free tier
            var latestTrade = await _alpacaDataClient.GetLatestTradeAsync(latestRequest);

            if (latestTrade == null)
            {
                _logger.LogWarning("No trade data available for {Symbol}", symbol);
                return null;
            }

            // Get latest quote for bid/ask spread info
            try
            {
                var latestQuote = await _alpacaDataClient.GetLatestQuoteAsync(latestRequest);

                return new StockQuote
                {
                    Symbol = symbol,
                    Price = latestTrade.Price,
                    Open = latestTrade.Price, // Use trade price as fallback
                    High = latestTrade.Price,
                    Low = latestTrade.Price,
                    Volume = 0, // Volume not available in latest trade
                    Timestamp = latestTrade.TimestampUtc ?? DateTime.UtcNow
                };
            }
            catch (Exception)
            {
                // If quote fails, just use trade data
                return new StockQuote
                {
                    Symbol = symbol,
                    Price = latestTrade.Price,
                    Open = latestTrade.Price,
                    High = latestTrade.Price,
                    Low = latestTrade.Price,
                    Volume = 0,
                    Timestamp = latestTrade.TimestampUtc ?? DateTime.UtcNow
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching quote for {Symbol}", symbol);
            return null;
        }
    }

    public async Task<List<StockQuote>> GetHistoricalData(string symbol, DateTime startDate, DateTime endDate)
    {
        try
        {
            // Make sure we're not requesting same-day data
            // Free tier only allows historical data from previous days
            var adjustedEndDate = endDate.Date < DateTime.UtcNow.Date
                ? endDate
                : DateTime.UtcNow.Date.AddDays(-1);

            var request = new HistoricalBarsRequest(symbol, startDate, adjustedEndDate, BarTimeFrame.Day);
            var bars = await _alpacaDataClient.ListHistoricalBarsAsync(request);

            return bars.Items.Select(bar => new StockQuote
            {
                Symbol = symbol,
                Price = bar.Close,
                Open = bar.Open,
                High = bar.High,
                Low = bar.Low,
                Volume = (long)bar.Volume,
                Timestamp = bar.TimeUtc
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching historical data for {Symbol}", symbol);
            return new List<StockQuote>();
        }
    }
}