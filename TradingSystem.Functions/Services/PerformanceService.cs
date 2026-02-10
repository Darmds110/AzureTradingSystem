using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradingSystem.Functions.Data;
using TradingSystem.Functions.Models;
using TradingSystem.Functions.Services.Interfaces;

namespace TradingSystem.Functions.Services
{
    public class PerformanceService : IPerformanceService
    {
        private readonly TradingDbContext _dbContext;
        private readonly ILogger<PerformanceService> _logger;
        private const decimal RiskFreeRate = 0.05m;

        public PerformanceService(TradingDbContext dbContext, ILogger<PerformanceService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<PerformanceMetrics> CalculateDailyMetricsAsync(int portfolioId)
        {
            var portfolio = await _dbContext.Portfolios.FindAsync(portfolioId)
                ?? throw new InvalidOperationException($"Portfolio {portfolioId} not found");

            var today = DateTime.UtcNow.Date;

            var yesterdayMetrics = await _dbContext.PerformanceMetrics
                .Where(m => m.PortfolioId == portfolioId && m.PeriodType == "DAILY" && m.MetricDate < today)
                .OrderByDescending(m => m.MetricDate)
                .FirstOrDefaultAsync();

            var previousValue = yesterdayMetrics?.PortfolioValue ?? portfolio.InitialCapital;
            var currentValue = portfolio.CurrentEquity;

            var dailyReturn = previousValue > 0 ? ((currentValue - previousValue) / previousValue) * 100 : 0;
            var totalReturn = portfolio.InitialCapital > 0 ? ((currentValue - portfolio.InitialCapital) / portfolio.InitialCapital) * 100 : 0;
            var maxDrawdown = portfolio.PeakValue > 0 ? ((portfolio.PeakValue - currentValue) / portfolio.PeakValue) * 100 : 0;

            var todayTrades = await _dbContext.TradeHistory
                .Where(t => t.PortfolioId == portfolioId && t.ExitDate.Date == today)
                .ToListAsync();

            var winningTrades = todayTrades.Count(t => t.RealizedProfitLoss > 0);
            var losingTrades = todayTrades.Count(t => t.RealizedProfitLoss < 0);
            decimal? winRate = todayTrades.Count > 0 ? (decimal)winningTrades / todayTrades.Count * 100 : null;

            var metrics = new PerformanceMetrics
            {
                PortfolioId = portfolioId,
                MetricDate = today,
                PeriodType = "DAILY",
                PortfolioValue = currentValue,
                TotalReturnPercent = totalReturn,
                PeriodReturnPercent = dailyReturn,
                MaxDrawdownPercent = maxDrawdown,
                TotalTrades = todayTrades.Count,
                WinningTrades = winningTrades,
                LosingTrades = losingTrades,
                WinRate = winRate,
                CreatedAt = DateTime.UtcNow
            };

            var existingMetrics = await _dbContext.PerformanceMetrics
                .FirstOrDefaultAsync(m => m.PortfolioId == portfolioId && m.PeriodType == "DAILY" && m.MetricDate == today);

            if (existingMetrics != null)
            {
                existingMetrics.PortfolioValue = metrics.PortfolioValue;
                existingMetrics.TotalReturnPercent = metrics.TotalReturnPercent;
                existingMetrics.PeriodReturnPercent = metrics.PeriodReturnPercent;
                existingMetrics.MaxDrawdownPercent = metrics.MaxDrawdownPercent;
                existingMetrics.TotalTrades = metrics.TotalTrades;
                existingMetrics.WinningTrades = metrics.WinningTrades;
                existingMetrics.LosingTrades = metrics.LosingTrades;
                existingMetrics.WinRate = metrics.WinRate;
                metrics = existingMetrics;
            }
            else
            {
                _dbContext.PerformanceMetrics.Add(metrics);
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Daily metrics calculated: Value={value}, DailyReturn={daily}%, TotalReturn={total}%",
                currentValue, dailyReturn, totalReturn);

            return metrics;
        }

        public async Task<PerformanceMetrics> CalculateWeeklyMetricsAsync(int portfolioId)
        {
            var portfolio = await _dbContext.Portfolios.FindAsync(portfolioId)
                ?? throw new InvalidOperationException($"Portfolio {portfolioId} not found");

            var today = DateTime.UtcNow.Date;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);

            var lastWeekMetrics = await _dbContext.PerformanceMetrics
                .Where(m => m.PortfolioId == portfolioId && m.PeriodType == "WEEKLY" && m.MetricDate < weekStart)
                .OrderByDescending(m => m.MetricDate)
                .FirstOrDefaultAsync();

            var previousValue = lastWeekMetrics?.PortfolioValue ?? portfolio.InitialCapital;
            var currentValue = portfolio.CurrentEquity;

            var weeklyReturn = previousValue > 0 ? ((currentValue - previousValue) / previousValue) * 100 : 0;
            var totalReturn = portfolio.InitialCapital > 0 ? ((currentValue - portfolio.InitialCapital) / portfolio.InitialCapital) * 100 : 0;

            var weekTrades = await _dbContext.TradeHistory
                .Where(t => t.PortfolioId == portfolioId && t.ExitDate >= weekStart)
                .ToListAsync();

            var winningTrades = weekTrades.Count(t => t.RealizedProfitLoss > 0);
            decimal? winRate = weekTrades.Count > 0 ? (decimal)winningTrades / weekTrades.Count * 100 : null;

            var sharpeRatio = await CalculateSharpeRatioAsync(portfolioId, weekStart, today);
            var maxDrawdown = portfolio.PeakValue > 0 ? ((portfolio.PeakValue - currentValue) / portfolio.PeakValue) * 100 : 0;

            var metrics = new PerformanceMetrics
            {
                PortfolioId = portfolioId,
                MetricDate = today,
                PeriodType = "WEEKLY",
                PortfolioValue = currentValue,
                TotalReturnPercent = totalReturn,
                PeriodReturnPercent = weeklyReturn,
                MaxDrawdownPercent = maxDrawdown,
                SharpeRatio = sharpeRatio,
                TotalTrades = weekTrades.Count,
                WinningTrades = winningTrades,
                LosingTrades = weekTrades.Count - winningTrades,
                WinRate = winRate,
                CreatedAt = DateTime.UtcNow
            };

            var existingMetrics = await _dbContext.PerformanceMetrics
                .FirstOrDefaultAsync(m => m.PortfolioId == portfolioId && m.PeriodType == "WEEKLY" && m.MetricDate >= weekStart && m.MetricDate <= today);

            if (existingMetrics != null)
            {
                existingMetrics.PortfolioValue = metrics.PortfolioValue;
                existingMetrics.TotalReturnPercent = metrics.TotalReturnPercent;
                existingMetrics.PeriodReturnPercent = metrics.PeriodReturnPercent;
                existingMetrics.MaxDrawdownPercent = metrics.MaxDrawdownPercent;
                existingMetrics.SharpeRatio = metrics.SharpeRatio;
                existingMetrics.TotalTrades = metrics.TotalTrades;
                existingMetrics.WinningTrades = metrics.WinningTrades;
                existingMetrics.LosingTrades = metrics.LosingTrades;
                existingMetrics.WinRate = metrics.WinRate;
                metrics = existingMetrics;
            }
            else
            {
                _dbContext.PerformanceMetrics.Add(metrics);
            }

            await _dbContext.SaveChangesAsync();
            return metrics;
        }

        public async Task<PerformanceMetrics> CalculateMonthlyMetricsAsync(int portfolioId)
        {
            var portfolio = await _dbContext.Portfolios.FindAsync(portfolioId)
                ?? throw new InvalidOperationException($"Portfolio {portfolioId} not found");

            var today = DateTime.UtcNow.Date;
            var monthStart = new DateTime(today.Year, today.Month, 1);

            var lastMonthMetrics = await _dbContext.PerformanceMetrics
                .Where(m => m.PortfolioId == portfolioId && m.PeriodType == "MONTHLY" && m.MetricDate < monthStart)
                .OrderByDescending(m => m.MetricDate)
                .FirstOrDefaultAsync();

            var previousValue = lastMonthMetrics?.PortfolioValue ?? portfolio.InitialCapital;
            var currentValue = portfolio.CurrentEquity;

            var monthlyReturn = previousValue > 0 ? ((currentValue - previousValue) / previousValue) * 100 : 0;
            var totalReturn = portfolio.InitialCapital > 0 ? ((currentValue - portfolio.InitialCapital) / portfolio.InitialCapital) * 100 : 0;

            var monthTrades = await _dbContext.TradeHistory
                .Where(t => t.PortfolioId == portfolioId && t.ExitDate >= monthStart)
                .ToListAsync();

            var winningTrades = monthTrades.Count(t => t.RealizedProfitLoss > 0);
            decimal? winRate = monthTrades.Count > 0 ? (decimal)winningTrades / monthTrades.Count * 100 : null;

            var sharpeRatio = await CalculateSharpeRatioAsync(portfolioId, monthStart, today);
            var maxDrawdown = await CalculateMaxDrawdownAsync(portfolioId, monthStart, today);

            var metrics = new PerformanceMetrics
            {
                PortfolioId = portfolioId,
                MetricDate = today,
                PeriodType = "MONTHLY",
                PortfolioValue = currentValue,
                TotalReturnPercent = totalReturn,
                PeriodReturnPercent = monthlyReturn,
                MaxDrawdownPercent = maxDrawdown,
                SharpeRatio = sharpeRatio,
                TotalTrades = monthTrades.Count,
                WinningTrades = winningTrades,
                LosingTrades = monthTrades.Count - winningTrades,
                WinRate = winRate,
                CreatedAt = DateTime.UtcNow
            };

            var existingMetrics = await _dbContext.PerformanceMetrics
                .FirstOrDefaultAsync(m => m.PortfolioId == portfolioId && m.PeriodType == "MONTHLY" && m.MetricDate >= monthStart && m.MetricDate <= today);

            if (existingMetrics != null)
            {
                existingMetrics.PortfolioValue = metrics.PortfolioValue;
                existingMetrics.TotalReturnPercent = metrics.TotalReturnPercent;
                existingMetrics.PeriodReturnPercent = metrics.PeriodReturnPercent;
                existingMetrics.MaxDrawdownPercent = metrics.MaxDrawdownPercent;
                existingMetrics.SharpeRatio = metrics.SharpeRatio;
                existingMetrics.TotalTrades = metrics.TotalTrades;
                existingMetrics.WinningTrades = metrics.WinningTrades;
                existingMetrics.LosingTrades = metrics.LosingTrades;
                existingMetrics.WinRate = metrics.WinRate;
                metrics = existingMetrics;
            }
            else
            {
                _dbContext.PerformanceMetrics.Add(metrics);
            }

            await _dbContext.SaveChangesAsync();
            return metrics;
        }

        public async Task<BenchmarkComparison> CompareToBenchmarksAsync(int portfolioId, DateTime date)
        {
            var portfolio = await _dbContext.Portfolios.FindAsync(portfolioId)
                ?? throw new InvalidOperationException($"Portfolio {portfolioId} not found");

            var portfolioReturn = portfolio.InitialCapital > 0
                ? ((portfolio.CurrentEquity - portfolio.InitialCapital) / portfolio.InitialCapital) * 100 : 0;

            var spyReturn = await GetBenchmarkReturnAsync("SPY", date);
            var qqqReturn = await GetBenchmarkReturnAsync("QQQ", date);

            return new BenchmarkComparison
            {
                PortfolioReturn = portfolioReturn,
                SpyReturn = spyReturn,
                QqqReturn = qqqReturn,
                AlphaVsSpy = portfolioReturn - spyReturn,
                AlphaVsQqq = portfolioReturn - qqqReturn
            };
        }

        public async Task CalculateStrategyPerformanceAsync(int portfolioId)
        {
            var strategies = await _dbContext.StrategyConfigurations.Where(s => s.IsActive).ToListAsync();

            foreach (var strategy in strategies)
            {
                var trades = await _dbContext.TradeHistory
                    .Where(t => t.PortfolioId == portfolioId && t.StrategyId == strategy.StrategyId)
                    .ToListAsync();

                if (trades.Count == 0) continue;

                var winningTrades = trades.Count(t => t.RealizedProfitLoss > 0);
                var winRate = (decimal)winningTrades / trades.Count * 100;
                var totalPL = trades.Sum(t => t.RealizedProfitLoss);
                var avgHoldingPeriod = trades.Average(t => t.HoldingPeriodDays);

                _logger.LogInformation("Strategy {name} performance: Trades={trades}, WinRate={winRate}%, P/L={pl}",
                    strategy.StrategyName, trades.Count, winRate, totalPL);
            }
        }

        public async Task<decimal> CalculateSharpeRatioAsync(int portfolioId, DateTime startDate, DateTime endDate)
        {
            var dailyReturns = await _dbContext.PerformanceMetrics
                .Where(m => m.PortfolioId == portfolioId && m.PeriodType == "DAILY" && m.MetricDate >= startDate && m.MetricDate <= endDate)
                .Select(m => m.PeriodReturnPercent)
                .ToListAsync();

            if (dailyReturns.Count < 2) return 0;

            var avgReturn = dailyReturns.Average();
            var variance = dailyReturns.Sum(r => (r - avgReturn) * (r - avgReturn)) / (dailyReturns.Count - 1);
            var stdDev = (decimal)Math.Sqrt((double)variance);

            if (stdDev == 0) return 0;

            var dailyRiskFree = RiskFreeRate / 252;
            var annualizedSharpe = ((avgReturn - dailyRiskFree) / stdDev) * (decimal)Math.Sqrt(252);

            return Math.Round(annualizedSharpe, 2);
        }

        public async Task<decimal> CalculateMaxDrawdownAsync(int portfolioId, DateTime startDate, DateTime endDate)
        {
            var portfolioValues = await _dbContext.PerformanceMetrics
                .Where(m => m.PortfolioId == portfolioId && m.PeriodType == "DAILY" && m.MetricDate >= startDate && m.MetricDate <= endDate)
                .OrderBy(m => m.MetricDate)
                .Select(m => m.PortfolioValue)
                .ToListAsync();

            if (portfolioValues.Count == 0) return 0;

            decimal maxDrawdown = 0;
            decimal peak = portfolioValues[0];

            foreach (var value in portfolioValues)
            {
                if (value > peak) peak = value;
                var drawdown = peak > 0 ? ((peak - value) / peak) * 100 : 0;
                if (drawdown > maxDrawdown) maxDrawdown = drawdown;
            }

            return Math.Round(maxDrawdown, 2);
        }

        public async Task<TradeStatistics> GetTradeStatisticsAsync(int portfolioId, DateTime startDate, DateTime endDate)
        {
            var trades = await _dbContext.TradeHistory
                .Where(t => t.PortfolioId == portfolioId && t.ExitDate >= startDate && t.ExitDate <= endDate)
                .ToListAsync();

            var winningTrades = trades.Where(t => t.RealizedProfitLoss > 0).ToList();
            var losingTrades = trades.Where(t => t.RealizedProfitLoss < 0).ToList();

            var avgGain = winningTrades.Count > 0 ? winningTrades.Average(t => t.RealizedProfitLoss) : 0;
            var avgLoss = losingTrades.Count > 0 ? Math.Abs(losingTrades.Average(t => t.RealizedProfitLoss)) : 0;
            var profitFactor = avgLoss > 0 ? avgGain / avgLoss : 0;

            return new TradeStatistics
            {
                TotalTrades = trades.Count,
                WinningTrades = winningTrades.Count,
                LosingTrades = losingTrades.Count,
                WinRatePercent = trades.Count > 0 ? (decimal)winningTrades.Count / trades.Count * 100 : 0,
                AverageGain = avgGain,
                AverageLoss = avgLoss,
                LargestGain = winningTrades.Count > 0 ? winningTrades.Max(t => t.RealizedProfitLoss) : 0,
                LargestLoss = losingTrades.Count > 0 ? Math.Abs(losingTrades.Min(t => t.RealizedProfitLoss)) : 0,
                ProfitFactor = profitFactor,
                AverageHoldingPeriodDays = trades.Count > 0 ? (decimal)trades.Average(t => t.HoldingPeriodDays) : 0,
                ExpectedValue = trades.Count > 0 ? trades.Average(t => t.RealizedProfitLoss) : 0
            };
        }

        public async Task<PerformanceMetrics?> GetLatestMetricsAsync(int portfolioId)
        {
            return await _dbContext.PerformanceMetrics
                .Where(m => m.PortfolioId == portfolioId && m.PeriodType == "DAILY")
                .OrderByDescending(m => m.MetricDate)
                .FirstOrDefaultAsync();
        }

        public async Task<List<PerformanceMetrics>> GetHistoricalMetricsAsync(int portfolioId, string periodType, int count)
        {
            return await _dbContext.PerformanceMetrics
                .Where(m => m.PortfolioId == portfolioId && m.PeriodType == periodType)
                .OrderByDescending(m => m.MetricDate)
                .Take(count)
                .OrderBy(m => m.MetricDate)
                .ToListAsync();
        }

        private async Task<decimal> GetBenchmarkReturnAsync(string symbol, DateTime date)
        {
            var oldestData = await _dbContext.MarketData
                .Where(m => m.Symbol == symbol)
                .OrderBy(m => m.DataTimestamp)
                .FirstOrDefaultAsync();

            var latestData = await _dbContext.MarketData
                .Where(m => m.Symbol == symbol && m.DataTimestamp.Date <= date)
                .OrderByDescending(m => m.DataTimestamp)
                .FirstOrDefaultAsync();

            if (oldestData == null || latestData == null || oldestData.ClosePrice == 0) return 0;

            return ((latestData.ClosePrice - oldestData.ClosePrice) / oldestData.ClosePrice) * 100;
        }
    }
}