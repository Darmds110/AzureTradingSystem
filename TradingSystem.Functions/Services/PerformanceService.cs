using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradingSystem.Functions.Data;
using TradingSystem.Functions.Models;
using TradingSystem.Functions.Services.Interfaces;

namespace TradingSystem.Functions.Services
{
    /// <summary>
    /// Implementation of performance service for calculating portfolio metrics
    /// </summary>
    public class PerformanceService : IPerformanceService
    {
        private readonly TradingDbContext _dbContext;
        private readonly ILogger<PerformanceService> _logger;

        // Risk-free rate for Sharpe ratio calculation (approximate T-bill rate)
        private const decimal RISK_FREE_RATE = 0.05m; // 5% annual

        public PerformanceService(
            TradingDbContext dbContext,
            ILogger<PerformanceService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// Calculates and stores daily performance metrics
        /// </summary>
        public async Task<PerformanceMetrics> CalculateDailyMetricsAsync(int portfolioId)
        {
            _logger.LogInformation("Calculating daily metrics for portfolio {id}", portfolioId);

            var portfolio = await _dbContext.Portfolios.FindAsync(portfolioId);
            if (portfolio == null)
            {
                throw new InvalidOperationException($"Portfolio {portfolioId} not found");
            }

            var today = DateTime.UtcNow.Date;
            var yesterday = today.AddDays(-1);

            // Get yesterday's metrics to calculate daily return
            var yesterdayMetrics = await _dbContext.PerformanceMetrics
                .Where(m => m.PortfolioId == portfolioId && m.MetricDate == yesterday && m.PeriodType == "DAILY")
                .FirstOrDefaultAsync();

            decimal previousValue = yesterdayMetrics?.PortfolioValue ?? portfolio.InitialCapital;
            decimal currentValue = portfolio.CurrentEquity;

            // Calculate daily return
            decimal dailyReturn = previousValue > 0
                ? ((currentValue - previousValue) / previousValue) * 100
                : 0;

            // Calculate total return from initial capital
            decimal totalReturn = portfolio.InitialCapital > 0
                ? ((currentValue - portfolio.InitialCapital) / portfolio.InitialCapital) * 100
                : 0;

            // Calculate drawdown
            decimal drawdown = portfolio.PeakValue > 0
                ? ((currentValue - portfolio.PeakValue) / portfolio.PeakValue) * 100
                : 0;

            // Get trade statistics for today
            var todayStats = await GetTradeStatisticsAsync(portfolioId, today, today);

            // Create metrics record
            var metrics = new PerformanceMetrics
            {
                PortfolioId = portfolioId,
                MetricDate = today,
                PeriodType = "DAILY",
                PortfolioValue = currentValue,
                CashBalance = portfolio.CurrentCash,
                PositionsValue = currentValue - portfolio.CurrentCash,
                PeriodReturnPercent = dailyReturn,
                TotalReturnPercent = totalReturn,
                Drawdown = drawdown,
                MaxDrawdownPercent = Math.Min(drawdown, yesterdayMetrics?.MaxDrawdownPercent ?? 0),
                PeakValue = portfolio.PeakValue,
                WinRate = todayStats.WinRatePercent,
                TotalTrades = todayStats.TotalTrades,
                WinningTrades = todayStats.WinningTrades,
                LosingTrades = todayStats.LosingTrades,
                AverageWin = todayStats.AverageGain,
                AverageLoss = todayStats.AverageLoss,
                CreatedAt = DateTime.UtcNow
            };

            // Check if metrics already exist for today
            var existingMetrics = await _dbContext.PerformanceMetrics
                .FirstOrDefaultAsync(m => m.PortfolioId == portfolioId && m.MetricDate == today && m.PeriodType == "DAILY");

            if (existingMetrics != null)
            {
                // Update existing
                existingMetrics.PortfolioValue = metrics.PortfolioValue;
                existingMetrics.CashBalance = metrics.CashBalance;
                existingMetrics.PositionsValue = metrics.PositionsValue;
                existingMetrics.PeriodReturnPercent = metrics.PeriodReturnPercent;
                existingMetrics.TotalReturnPercent = metrics.TotalReturnPercent;
                existingMetrics.Drawdown = metrics.Drawdown;
                existingMetrics.MaxDrawdownPercent = metrics.MaxDrawdownPercent;
                existingMetrics.PeakValue = metrics.PeakValue;
                existingMetrics.WinRate = metrics.WinRate;
                existingMetrics.TotalTrades = metrics.TotalTrades;
                metrics = existingMetrics;
            }
            else
            {
                _dbContext.PerformanceMetrics.Add(metrics);
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Daily metrics saved: Value=${value:N2}, Daily={daily:F2}%, Total={total:F2}%",
                currentValue, dailyReturn, totalReturn);

            return metrics;
        }

        /// <summary>
        /// Calculates and stores weekly performance metrics
        /// </summary>
        public async Task<PerformanceMetrics> CalculateWeeklyMetricsAsync(int portfolioId)
        {
            _logger.LogInformation("Calculating weekly metrics for portfolio {id}", portfolioId);

            var portfolio = await _dbContext.Portfolios.FindAsync(portfolioId);
            if (portfolio == null)
            {
                throw new InvalidOperationException($"Portfolio {portfolioId} not found");
            }

            var today = DateTime.UtcNow.Date;
            var weekStart = today.AddDays(-(int)today.DayOfWeek); // Start of week (Sunday)
            var lastWeekEnd = weekStart.AddDays(-1);

            // Get last week's ending value
            var lastWeekMetrics = await _dbContext.PerformanceMetrics
                .Where(m => m.PortfolioId == portfolioId && m.MetricDate <= lastWeekEnd && m.PeriodType == "WEEKLY")
                .OrderByDescending(m => m.MetricDate)
                .FirstOrDefaultAsync();

            decimal previousValue = lastWeekMetrics?.PortfolioValue ?? portfolio.InitialCapital;
            decimal currentValue = portfolio.CurrentEquity;

            // Calculate weekly return
            decimal weeklyReturn = previousValue > 0
                ? ((currentValue - previousValue) / previousValue) * 100
                : 0;

            // Get trade statistics for the week
            var weekStats = await GetTradeStatisticsAsync(portfolioId, weekStart, today);

            // Calculate Sharpe ratio for the week
            var sharpeRatio = await CalculateSharpeRatioAsync(portfolioId, weekStart, today);

            var metrics = new PerformanceMetrics
            {
                PortfolioId = portfolioId,
                MetricDate = today,
                PeriodType = "WEEKLY",
                PortfolioValue = currentValue,
                CashBalance = portfolio.CurrentCash,
                PositionsValue = currentValue - portfolio.CurrentCash,
                PeriodReturnPercent = weeklyReturn,
                TotalReturnPercent = ((currentValue - portfolio.InitialCapital) / portfolio.InitialCapital) * 100,
                Drawdown = portfolio.CurrentDrawdownPercent,
                MaxDrawdownPercent = await CalculateMaxDrawdownAsync(portfolioId, weekStart, today),
                PeakValue = portfolio.PeakValue,
                SharpeRatio = sharpeRatio,
                WinRate = weekStats.WinRatePercent,
                TotalTrades = weekStats.TotalTrades,
                WinningTrades = weekStats.WinningTrades,
                LosingTrades = weekStats.LosingTrades,
                AverageWin = weekStats.AverageGain,
                AverageLoss = weekStats.AverageLoss,
                AverageHoldingPeriod = weekStats.AverageHoldingPeriodDays,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.PerformanceMetrics.Add(metrics);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Weekly metrics saved: Return={return:F2}%, Sharpe={sharpe:F2}, WinRate={winRate:F1}%",
                weeklyReturn, sharpeRatio, weekStats.WinRatePercent);

            return metrics;
        }

        /// <summary>
        /// Calculates and stores monthly performance metrics
        /// </summary>
        public async Task<PerformanceMetrics> CalculateMonthlyMetricsAsync(int portfolioId)
        {
            _logger.LogInformation("Calculating monthly metrics for portfolio {id}", portfolioId);

            var portfolio = await _dbContext.Portfolios.FindAsync(portfolioId);
            if (portfolio == null)
            {
                throw new InvalidOperationException($"Portfolio {portfolioId} not found");
            }

            var today = DateTime.UtcNow.Date;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var lastMonthEnd = monthStart.AddDays(-1);

            // Get last month's ending value
            var lastMonthMetrics = await _dbContext.PerformanceMetrics
                .Where(m => m.PortfolioId == portfolioId && m.MetricDate <= lastMonthEnd && m.PeriodType == "MONTHLY")
                .OrderByDescending(m => m.MetricDate)
                .FirstOrDefaultAsync();

            decimal previousValue = lastMonthMetrics?.PortfolioValue ?? portfolio.InitialCapital;
            decimal currentValue = portfolio.CurrentEquity;

            // Calculate monthly return
            decimal monthlyReturn = previousValue > 0
                ? ((currentValue - previousValue) / previousValue) * 100
                : 0;

            // Get trade statistics for the month
            var monthStats = await GetTradeStatisticsAsync(portfolioId, monthStart, today);

            // Calculate metrics
            var sharpeRatio = await CalculateSharpeRatioAsync(portfolioId, monthStart, today);
            var maxDrawdown = await CalculateMaxDrawdownAsync(portfolioId, monthStart, today);

            var metrics = new PerformanceMetrics
            {
                PortfolioId = portfolioId,
                MetricDate = today,
                PeriodType = "MONTHLY",
                PortfolioValue = currentValue,
                CashBalance = portfolio.CurrentCash,
                PositionsValue = currentValue - portfolio.CurrentCash,
                PeriodReturnPercent = monthlyReturn,
                TotalReturnPercent = ((currentValue - portfolio.InitialCapital) / portfolio.InitialCapital) * 100,
                Drawdown = portfolio.CurrentDrawdownPercent,
                MaxDrawdownPercent = maxDrawdown,
                PeakValue = portfolio.PeakValue,
                SharpeRatio = sharpeRatio,
                WinRate = monthStats.WinRatePercent,
                TotalTrades = monthStats.TotalTrades,
                WinningTrades = monthStats.WinningTrades,
                LosingTrades = monthStats.LosingTrades,
                AverageWin = monthStats.AverageGain,
                AverageLoss = monthStats.AverageLoss,
                AverageHoldingPeriod = monthStats.AverageHoldingPeriodDays,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.PerformanceMetrics.Add(metrics);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Monthly metrics saved: Return={return:F2}%, Sharpe={sharpe:F2}, MaxDD={maxDD:F2}%",
                monthlyReturn, sharpeRatio, maxDrawdown);

            return metrics;
        }

        /// <summary>
        /// Compares portfolio performance against benchmarks
        /// </summary>
        public async Task<BenchmarkComparison> CompareToBenchmarksAsync(int portfolioId, DateTime asOfDate)
        {
            var portfolio = await _dbContext.Portfolios.FindAsync(portfolioId);
            if (portfolio == null)
            {
                throw new InvalidOperationException($"Portfolio {portfolioId} not found");
            }

            // Calculate portfolio return
            decimal portfolioReturn = portfolio.InitialCapital > 0
                ? ((portfolio.CurrentEquity - portfolio.InitialCapital) / portfolio.InitialCapital) * 100
                : 0;

            // Get benchmark returns from market data
            var spyReturn = await GetBenchmarkReturnAsync("SPY", portfolio.CreatedAt, asOfDate);
            var qqqReturn = await GetBenchmarkReturnAsync("QQQ", portfolio.CreatedAt, asOfDate);

            var comparison = new BenchmarkComparison
            {
                PortfolioReturn = portfolioReturn,
                SpyReturn = spyReturn,
                QqqReturn = qqqReturn,
                AlphaVsSpy = portfolioReturn - spyReturn,
                AlphaVsQqq = portfolioReturn - qqqReturn,
            };

            _logger.LogInformation(
                "Benchmark comparison: Portfolio={port:F2}%, SPY={spy:F2}%, QQQ={qqq:F2}%, Alpha={alpha:F2}%",
                portfolioReturn, spyReturn, qqqReturn, comparison.AlphaVsSpy);

            return comparison;
        }

        /// <summary>
        /// Gets benchmark return from market data
        /// </summary>
        private async Task<decimal> GetBenchmarkReturnAsync(string symbol, DateTime startDate, DateTime endDate)
        {
            var startData = await _dbContext.MarketData
                .Where(m => m.Symbol == symbol && m.DataDate >= startDate.Date)
                .OrderBy(m => m.DataDate)
                .FirstOrDefaultAsync();

            var endData = await _dbContext.MarketData
                .Where(m => m.Symbol == symbol && m.DataDate <= endDate.Date)
                .OrderByDescending(m => m.DataDate)
                .FirstOrDefaultAsync();

            if (startData == null || endData == null || startData.ClosePrice == 0)
            {
                return 0;
            }

            return ((endData.ClosePrice - startData.ClosePrice) / startData.ClosePrice) * 100;
        }

        /// <summary>
        /// Calculates performance metrics for each strategy
        /// </summary>
        public async Task CalculateStrategyPerformanceAsync(int portfolioId)
        {
            _logger.LogInformation("Calculating strategy performance for portfolio {id}", portfolioId);

            var strategies = await _dbContext.StrategyConfigurations
                .Where(s => s.IsActive)
                .ToListAsync();

            foreach (var strategy in strategies)
            {
                var trades = await _dbContext.TradeHistory
                    .Where(t => t.PortfolioId == portfolioId && t.StrategyId == strategy.StrategyId)
                    .ToListAsync();

                if (!trades.Any()) continue;

                var winningTrades = trades.Where(t => (t.RealizedProfitLoss ?? 0) > 0).ToList();
                var losingTrades = trades.Where(t => (t.RealizedProfitLoss ?? 0) <= 0).ToList();

                decimal winRate = trades.Count > 0 ? (decimal)winningTrades.Count / trades.Count * 100 : 0;
                decimal totalReturn = trades.Sum(t => t.RealizedProfitLoss ?? 0);
                decimal avgHoldingPeriod = trades.Any() ? (decimal)trades.Average(t => t.HoldingPeriodDays ?? 0) : 0;

                _logger.LogInformation(
                    "Strategy {name}: Trades={count}, WinRate={winRate:F1}%, TotalReturn=${return:N2}",
                    strategy.StrategyName, trades.Count, winRate, totalReturn);
            }
        }

        /// <summary>
        /// Calculates Sharpe ratio
        /// </summary>
        public async Task<decimal> CalculateSharpeRatioAsync(int portfolioId, DateTime startDate, DateTime endDate)
        {
            var dailyMetrics = await _dbContext.PerformanceMetrics
                .Where(m => m.PortfolioId == portfolioId
                    && m.MetricDate >= startDate
                    && m.MetricDate <= endDate
                    && m.PeriodType == "DAILY")
                .OrderBy(m => m.MetricDate)
                .ToListAsync();

            if (dailyMetrics.Count < 2)
            {
                return 0;
            }

            var returns = dailyMetrics.Select(m => m.PeriodReturnPercent).ToList();
            var avgReturn = returns.Average();
            var stdDev = CalculateStandardDeviation(returns);

            if (stdDev == 0)
            {
                return 0;
            }

            // Annualize: multiply by sqrt(252) for daily data
            decimal annualizedReturn = avgReturn * 252;
            decimal annualizedStdDev = stdDev * (decimal)Math.Sqrt(252);

            decimal sharpeRatio = (annualizedReturn - RISK_FREE_RATE * 100) / annualizedStdDev;

            return Math.Round(sharpeRatio, 2);
        }

        /// <summary>
        /// Calculates maximum drawdown
        /// </summary>
        public async Task<decimal> CalculateMaxDrawdownAsync(int portfolioId, DateTime startDate, DateTime endDate)
        {
            var dailyMetrics = await _dbContext.PerformanceMetrics
                .Where(m => m.PortfolioId == portfolioId
                    && m.MetricDate >= startDate
                    && m.MetricDate <= endDate
                    && m.PeriodType == "DAILY")
                .OrderBy(m => m.MetricDate)
                .ToListAsync();

            if (!dailyMetrics.Any())
            {
                return 0;
            }

            decimal maxDrawdown = 0;
            decimal peak = dailyMetrics.First().PortfolioValue;

            foreach (var metric in dailyMetrics)
            {
                if (metric.PortfolioValue > peak)
                {
                    peak = metric.PortfolioValue;
                }

                decimal drawdown = peak > 0 ? ((metric.PortfolioValue - peak) / peak) * 100 : 0;

                if (drawdown < maxDrawdown)
                {
                    maxDrawdown = drawdown;
                }
            }

            return maxDrawdown;
        }

        /// <summary>
        /// Gets trade statistics for a period
        /// </summary>
        public async Task<TradeStatistics> GetTradeStatisticsAsync(int portfolioId, DateTime startDate, DateTime endDate)
        {
            var trades = await _dbContext.TradeHistory
                .Where(t => t.PortfolioId == portfolioId
                    && t.ExitDate.HasValue
                    && t.ExitDate.Value.Date >= startDate
                    && t.ExitDate.Value.Date <= endDate)
                .ToListAsync();

            var winningTrades = trades.Where(t => (t.RealizedProfitLoss ?? 0) > 0).ToList();
            var losingTrades = trades.Where(t => (t.RealizedProfitLoss ?? 0) <= 0).ToList();

            decimal totalGains = winningTrades.Sum(t => t.RealizedProfitLoss ?? 0);
            decimal totalLosses = Math.Abs(losingTrades.Sum(t => t.RealizedProfitLoss ?? 0));

            return new TradeStatistics
            {
                TotalTrades = trades.Count,
                WinningTrades = winningTrades.Count,
                LosingTrades = losingTrades.Count,
                WinRatePercent = trades.Count > 0 ? (decimal)winningTrades.Count / trades.Count * 100 : 0,
                AverageGain = winningTrades.Any() ? winningTrades.Average(t => t.RealizedProfitLoss ?? 0) : 0,
                AverageLoss = losingTrades.Any() ? Math.Abs(losingTrades.Average(t => t.RealizedProfitLoss ?? 0)) : 0,
                LargestGain = winningTrades.Any() ? winningTrades.Max(t => t.RealizedProfitLoss ?? 0) : 0,
                LargestLoss = losingTrades.Any() ? Math.Abs(losingTrades.Min(t => t.RealizedProfitLoss ?? 0)) : 0,
                ProfitFactor = totalLosses > 0 ? totalGains / totalLosses : totalGains > 0 ? decimal.MaxValue : 0,
                AverageHoldingPeriodDays = trades.Any() ? (decimal)trades.Average(t => t.HoldingPeriodDays ?? 0) : 0,
                ExpectedValue = trades.Count > 0
                    ? (winningTrades.Count / (decimal)trades.Count * (winningTrades.Any() ? winningTrades.Average(t => t.RealizedProfitLoss ?? 0) : 0))
                      - (losingTrades.Count / (decimal)trades.Count * (losingTrades.Any() ? Math.Abs(losingTrades.Average(t => t.RealizedProfitLoss ?? 0)) : 0))
                    : 0
            };
        }

        /// <summary>
        /// Gets the latest performance metrics
        /// </summary>
        public async Task<PerformanceMetrics?> GetLatestMetricsAsync(int portfolioId)
        {
            return await _dbContext.PerformanceMetrics
                .Where(m => m.PortfolioId == portfolioId && m.PeriodType == "DAILY")
                .OrderByDescending(m => m.MetricDate)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Gets historical metrics for charting
        /// </summary>
        public async Task<List<PerformanceMetrics>> GetHistoricalMetricsAsync(int portfolioId, string periodType, int count)
        {
            return await _dbContext.PerformanceMetrics
                .Where(m => m.PortfolioId == portfolioId && m.PeriodType == periodType)
                .OrderByDescending(m => m.MetricDate)
                .Take(count)
                .OrderBy(m => m.MetricDate)
                .ToListAsync();
        }

        /// <summary>
        /// Calculates standard deviation of a list of values
        /// </summary>
        private decimal CalculateStandardDeviation(List<decimal> values)
        {
            if (values.Count < 2) return 0;

            var avg = values.Average();
            var sumSquares = values.Sum(v => (v - avg) * (v - avg));
            var variance = sumSquares / (values.Count - 1);

            return (decimal)Math.Sqrt((double)variance);
        }
    }
}