using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradingSystem.Functions.Data;
using TradingSystem.Functions.Services.Interfaces;

namespace TradingSystem.Functions.Services;

public class TechnicalIndicatorsService : ITechnicalIndicatorsService
{
    private readonly TradingDbContext _dbContext;
    private readonly IMarketDataService _marketDataService;
    private readonly ILogger<TechnicalIndicatorsService> _logger;

    public TechnicalIndicatorsService(
        TradingDbContext dbContext,
        IMarketDataService marketDataService,
        ILogger<TechnicalIndicatorsService> logger)
    {
        _dbContext = dbContext;
        _marketDataService = marketDataService;
        _logger = logger;
    }

    public async Task<TechnicalIndicators> CalculateIndicators(string symbol, DateTime asOfDate)
    {
        try
        {
            // Get historical data (need 200+ days for SMA200)
            var startDate = asOfDate.AddDays(-250);
            var historicalData = await _marketDataService.GetHistoricalData(symbol, startDate, asOfDate);

            if (historicalData.Count < 14)
            {
                _logger.LogWarning("Insufficient data for {Symbol} to calculate indicators", symbol);
                return new TechnicalIndicators();
            }

            var closePrices = historicalData.Select(h => h.Price).ToList();

            var indicators = new TechnicalIndicators
            {
                SMA20 = CalculateSMA(closePrices, 20),
                SMA50 = CalculateSMA(closePrices, 50),
                SMA200 = CalculateSMA(closePrices, 200),
                EMA12 = CalculateEMA(closePrices, 12),
                EMA26 = CalculateEMA(closePrices, 26),
                RSI = CalculateRSI(closePrices, 14)
            };

            // Calculate MACD
            if (indicators.EMA12.HasValue && indicators.EMA26.HasValue)
            {
                indicators.MACD = indicators.EMA12.Value - indicators.EMA26.Value;
                indicators.MACDSignal = null;
                indicators.MACDHistogram = null;
            }

            return indicators;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating indicators for {Symbol}", symbol);
            return new TechnicalIndicators();
        }
    }

    private decimal? CalculateSMA(List<decimal> prices, int period)
    {
        if (prices.Count < period)
            return null;

        var recentPrices = prices.TakeLast(period).ToList();
        return recentPrices.Average();
    }

    private decimal? CalculateEMA(List<decimal> prices, int period)
    {
        if (prices.Count < period)
            return null;

        decimal multiplier = 2m / (period + 1);

        // Start with SMA for first value
        var sma = prices.Take(period).Average();
        decimal ema = sma;

        // Calculate EMA for remaining values
        for (int i = period; i < prices.Count; i++)
        {
            ema = (prices[i] - ema) * multiplier + ema;
        }

        return ema;
    }

    private decimal? CalculateRSI(List<decimal> prices, int period)
    {
        if (prices.Count < period + 1)
            return null;

        var changes = new List<decimal>();
        for (int i = 1; i < prices.Count; i++)
        {
            changes.Add(prices[i] - prices[i - 1]);
        }

        var recentChanges = changes.TakeLast(period).ToList();

        var gains = recentChanges.Where(c => c > 0).DefaultIfEmpty(0).Average();
        var losses = Math.Abs(recentChanges.Where(c => c < 0).DefaultIfEmpty(0).Average());

        if (losses == 0)
            return 100;

        var rs = gains / losses;
        var rsi = 100 - (100 / (1 + rs));

        return rsi;
    }
}