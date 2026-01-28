using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alpaca.Markets;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradingSystem.Functions.Config;
using TradingSystem.Functions.Data;
using TradingSystem.Functions.Models;

namespace TradingSystem.Functions.Functions
{
    public class HistoricalDataBackfill
    {
        private readonly TradingDbContext _dbContext;
        private readonly AlpacaConfig _alpacaConfig;
        private readonly ILogger<HistoricalDataBackfill> _logger;
        private IAlpacaDataClient? _alpacaDataClient;

        public HistoricalDataBackfill(
            TradingDbContext dbContext,
            AlpacaConfig alpacaConfig,
            ILogger<HistoricalDataBackfill> logger)
        {
            _dbContext = dbContext;
            _alpacaConfig = alpacaConfig;
            _logger = logger;
        }

        [Function("HistoricalDataBackfill")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("HistoricalDataBackfill function triggered");

            try
            {
                // Initialize Alpaca client
                var secretKey = new SecretKey(_alpacaConfig.ApiKey, _alpacaConfig.SecretKey);
                _alpacaDataClient = Environments.Paper.GetAlpacaDataClient(secretKey);

                // Watchlist of symbols
                var symbols = new List<string> { "AAPL", "MSFT", "GOOGL", "AMZN", "TSLA", "NVDA", "META", "SPY", "QQQ" };

                // Date range: 200 trading days back (approximately 10 months)
                var endDate = DateTime.UtcNow.Date;
                var startDate = endDate.AddDays(-300); // Get more days to ensure 200 trading days

                _logger.LogInformation($"Fetching historical data from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");

                int totalRecordsInserted = 0;
                var results = new List<string>();

                foreach (var symbol in symbols)
                {
                    try
                    {
                        _logger.LogInformation($"Processing {symbol}...");

                        // Fetch historical bars from Alpaca
                        var barsRequest = new HistoricalBarsRequest(symbol, startDate, endDate, BarTimeFrame.Day);
                        var barsPage = await _alpacaDataClient.ListHistoricalBarsAsync(barsRequest);
                        var bars = barsPage.Items.ToList();

                        _logger.LogInformation($"Retrieved {bars.Count} daily bars for {symbol}");

                        if (bars.Count == 0)
                        {
                            _logger.LogWarning($"No historical data found for {symbol}");
                            results.Add($"{symbol}: No data available");
                            continue;
                        }

                        // Sort by date ascending
                        bars = bars.OrderBy(b => b.TimeUtc).ToList();

                        // Get all existing data for this symbol to calculate indicators
                        var allPrices = bars.Select(b => b.Close).ToList();

                        // Calculate technical indicators for each bar
                        var marketDataList = new List<MarketData>();

                        for (int i = 0; i < bars.Count; i++)
                        {
                            var bar = bars[i];
                            var pricesUpToNow = allPrices.Take(i + 1).ToList();

                            // Calculate indicators
                            decimal? rsi = null;
                            decimal? sma20 = null;
                            decimal? sma50 = null;
                            decimal? sma200 = null;
                            decimal? ema12 = null;
                            decimal? ema26 = null;
                            decimal? macd = null;
                            decimal? macdSignal = null;
                            decimal? macdHistogram = null;

                            try
                            {
                                // Calculate RSI (14-period)
                                if (pricesUpToNow.Count >= 14)
                                {
                                    rsi = CalculateRSI(pricesUpToNow, 14);
                                }

                                // Calculate SMAs
                                if (pricesUpToNow.Count >= 20)
                                {
                                    sma20 = pricesUpToNow.TakeLast(20).Average();
                                }

                                if (pricesUpToNow.Count >= 50)
                                {
                                    sma50 = pricesUpToNow.TakeLast(50).Average();
                                }

                                if (pricesUpToNow.Count >= 200)
                                {
                                    sma200 = pricesUpToNow.TakeLast(200).Average();
                                }

                                // Calculate EMAs
                                if (pricesUpToNow.Count >= 12)
                                {
                                    ema12 = CalculateEMA(pricesUpToNow, 12);
                                }

                                if (pricesUpToNow.Count >= 26)
                                {
                                    ema26 = CalculateEMA(pricesUpToNow, 26);
                                }

                                // Calculate MACD
                                if (ema12.HasValue && ema26.HasValue && pricesUpToNow.Count >= 35)
                                {
                                    macd = ema12.Value - ema26.Value;

                                    // Calculate signal line (9-period EMA of MACD)
                                    if (i >= 34)
                                    {
                                        var macdValues = new List<decimal>();
                                        for (int j = Math.Max(0, i - 50); j <= i; j++)
                                        {
                                            if (j >= 25)
                                            {
                                                var prices = allPrices.Take(j + 1).ToList();
                                                var e12 = CalculateEMA(prices, 12);
                                                var e26 = CalculateEMA(prices, 26);
                                                if (e12.HasValue && e26.HasValue)
                                                {
                                                    macdValues.Add(e12.Value - e26.Value);
                                                }
                                            }
                                        }

                                        if (macdValues.Count >= 9)
                                        {
                                            macdSignal = CalculateEMA(macdValues, 9);
                                            if (macdSignal.HasValue)
                                            {
                                                macdHistogram = macd - macdSignal.Value;
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning($"Error calculating indicators for {symbol} on {bar.TimeUtc:yyyy-MM-dd}: {ex.Message}");
                            }

                            var marketData = new MarketData
                            {
                                Symbol = symbol,
                                DataTimestamp = bar.TimeUtc,
                                DataDate = bar.TimeUtc.Date,
                                OpenPrice = bar.Open,
                                HighPrice = bar.High,
                                LowPrice = bar.Low,
                                ClosePrice = bar.Close,
                                Volume = (long)bar.Volume,
                                RSI = rsi,
                                SMA20 = sma20,
                                SMA50 = sma50,
                                SMA200 = sma200,
                                EMA12 = ema12,
                                EMA26 = ema26,
                                MACD = macd,
                                MACDSignal = macdSignal,
                                MACDHistogram = macdHistogram,
                                CreatedAt = DateTime.UtcNow
                            };

                            marketDataList.Add(marketData);
                        }

                        // Bulk insert into database
                        await _dbContext.MarketData.AddRangeAsync(marketDataList);
                        await _dbContext.SaveChangesAsync();

                        totalRecordsInserted += marketDataList.Count;
                        results.Add($"{symbol}: Inserted {marketDataList.Count} records");

                        _logger.LogInformation($"Successfully inserted {marketDataList.Count} records for {symbol}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error processing {symbol}: {ex.Message}");
                        results.Add($"{symbol}: ERROR - {ex.Message}");
                    }

                    // Small delay to avoid rate limiting
                    await Task.Delay(1000);
                }

                _logger.LogInformation($"Historical data backfill complete. Total records inserted: {totalRecordsInserted}");

                // Create response
                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    success = true,
                    message = $"Historical data backfill completed successfully",
                    totalRecordsInserted = totalRecordsInserted,
                    symbolResults = results,
                    dateRange = new
                    {
                        start = startDate.ToString("yyyy-MM-dd"),
                        end = endDate.ToString("yyyy-MM-dd")
                    }
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Critical error in historical data backfill: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");

                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new
                {
                    success = false,
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });

                return errorResponse;
            }
        }

        // Helper method to calculate RSI
        private decimal? CalculateRSI(List<decimal> prices, int period)
        {
            if (prices.Count < period + 1)
                return null;

            var gains = new List<decimal>();
            var losses = new List<decimal>();

            for (int i = 1; i < prices.Count; i++)
            {
                var change = prices[i] - prices[i - 1];
                gains.Add(change > 0 ? change : 0);
                losses.Add(change < 0 ? Math.Abs(change) : 0);
            }

            var avgGain = gains.TakeLast(period).Average();
            var avgLoss = losses.TakeLast(period).Average();

            if (avgLoss == 0)
                return 100;

            var rs = avgGain / avgLoss;
            var rsi = 100 - (100 / (1 + rs));

            return rsi;
        }

        // Helper method to calculate EMA
        private decimal? CalculateEMA(List<decimal> prices, int period)
        {
            if (prices.Count < period)
                return null;

            var multiplier = 2.0m / (period + 1);
            var ema = prices.Take(period).Average();

            for (int i = period; i < prices.Count; i++)
            {
                ema = ((prices[i] - ema) * multiplier) + ema;
            }

            return ema;
        }
    }
}