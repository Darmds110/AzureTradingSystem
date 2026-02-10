using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradingSystem.Functions.Data;
using TradingSystem.Functions.Models;
using TradingSystem.Functions.Services.Interfaces;

namespace TradingSystem.Functions.Functions;

public class MarketDataCollector
{
    private readonly TradingDbContext _dbContext;
    private readonly IMarketDataService _marketDataService;
    private readonly ITechnicalIndicatorsService _indicators;
    private readonly ITableStorageService _tableStorage;
    private readonly IEmailService _emailService;
    private readonly ILogger<MarketDataCollector> _logger;

    // Watchlist of stocks to monitor
    private readonly string[] _watchlist = new[]
    {
        "AAPL", "MSFT", "GOOGL", "AMZN", "TSLA",
        "NVDA", "META", "SPY", "QQQ"
    };

    public MarketDataCollector(
        TradingDbContext dbContext,
        IMarketDataService marketDataService,
        ITechnicalIndicatorsService indicators,
        ITableStorageService tableStorage,
        IEmailService emailService,
        ILogger<MarketDataCollector> logger)
    {
        _dbContext = dbContext;
        _marketDataService = marketDataService;
        _indicators = indicators;
        _tableStorage = tableStorage;
        _emailService = emailService;
        _logger = logger;
    }

    [Function("MarketDataCollector")]
    public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo timer)
    {
        _logger.LogInformation("MarketDataCollector executed at: {Time}", DateTime.UtcNow);

        try
        {
            // Check if market is open
            var isMarketOpen = await _marketDataService.IsMarketOpen();

            if (!isMarketOpen)
            {
                _logger.LogInformation("Market is closed. Skipping data collection.");
                return;
            }

            _logger.LogInformation("Market is open. Collecting data for {Count} symbols", _watchlist.Length);

            var successCount = 0;
            var errorCount = 0;

            foreach (var symbol in _watchlist)
            {
                try
                {
                    await CollectDataForSymbol(symbol);
                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error collecting data for {Symbol}", symbol);
                    errorCount++;
                }
            }

            _logger.LogInformation(
                "Data collection complete. Success: {Success}, Errors: {Errors}",
                successCount,
                errorCount
            );

            if (errorCount > _watchlist.Length / 2)
            {
                await _emailService.SendErrorNotificationAsync(
                    $"High error rate in data collection: {errorCount}/{_watchlist.Length} symbols failed"
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error in MarketDataCollector");
            await _emailService.SendErrorNotificationAsync("MarketDataCollector failed", ex);
            throw;
        }
    }

    private async Task CollectDataForSymbol(string symbol)
    {
        // 1. Get latest quote
        var quote = await _marketDataService.GetLatestQuote(symbol);

        if (quote == null)
        {
            _logger.LogWarning("Failed to get quote for {Symbol}", symbol);
            return;
        }

        // 2. Calculate technical indicators
        var indicators = await _indicators.CalculateIndicators(symbol, DateTime.UtcNow);

        // 3. Save to SQL Database
        var marketData = new MarketData
        {
            Symbol = symbol,
            DataTimestamp = quote.Timestamp,
            DataDate = quote.Timestamp.Date,
            OpenPrice = quote.Open,
            HighPrice = quote.High,
            LowPrice = quote.Low,
            ClosePrice = quote.Price,
            Volume = quote.Volume,
            RSI = indicators.RSI,
            SMA20 = indicators.SMA20,
            SMA50 = indicators.SMA50,
            SMA200 = indicators.SMA200,
            EMA12 = indicators.EMA12,
            EMA26 = indicators.EMA26,
            MACD = indicators.MACD,
            MACDSignal = indicators.MACDSignal,
            MACDHistogram = indicators.MACDHistogram,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.MarketData.Add(marketData);
        await _dbContext.SaveChangesAsync();

        // 4. Cache latest quote in Table Storage - use Async version
        await _tableStorage.SaveLatestQuoteAsync(symbol, quote.Price, quote.Timestamp);

        _logger.LogInformation(
            "Collected data for {Symbol}: Price={Price}, RSI={RSI}, Volume={Volume}",
            symbol,
            quote.Price,
            indicators.RSI,
            quote.Volume
        );
    }
}