using TradingSystem.Functions.Models;

namespace TradingSystem.Functions.Services.Interfaces
{
    /// <summary>
    /// Service for calculating and storing performance metrics
    /// </summary>
    public interface IPerformanceService
    {
        /// <summary>
        /// Calculates daily performance metrics
        /// </summary>
        Task<PerformanceMetrics> CalculateDailyMetricsAsync(int portfolioId);

        /// <summary>
        /// Calculates weekly performance metrics
        /// </summary>
        Task<PerformanceMetrics> CalculateWeeklyMetricsAsync(int portfolioId);

        /// <summary>
        /// Calculates monthly performance metrics
        /// </summary>
        Task<PerformanceMetrics> CalculateMonthlyMetricsAsync(int portfolioId);

        /// <summary>
        /// Compares portfolio performance to benchmarks (SPY, QQQ)
        /// </summary>
        Task<BenchmarkComparison> CompareToBenchmarksAsync(int portfolioId, DateTime date);

        /// <summary>
        /// Calculates performance metrics per strategy
        /// </summary>
        Task CalculateStrategyPerformanceAsync(int portfolioId);

        /// <summary>
        /// Calculates Sharpe ratio for a period
        /// </summary>
        Task<decimal> CalculateSharpeRatioAsync(int portfolioId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Calculates maximum drawdown for a period
        /// </summary>
        Task<decimal> CalculateMaxDrawdownAsync(int portfolioId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Gets trade statistics for a period
        /// </summary>
        Task<TradeStatistics> GetTradeStatisticsAsync(int portfolioId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Gets the latest performance metrics
        /// </summary>
        Task<PerformanceMetrics?> GetLatestMetricsAsync(int portfolioId);

        /// <summary>
        /// Gets historical metrics for charting
        /// </summary>
        Task<List<PerformanceMetrics>> GetHistoricalMetricsAsync(int portfolioId, string periodType, int count);
    }

    /// <summary>
    /// Benchmark comparison results
    /// </summary>
    public class BenchmarkComparison
    {
        public decimal PortfolioReturn { get; set; }
        public decimal SpyReturn { get; set; }
        public decimal QqqReturn { get; set; }
        public decimal AlphaVsSpy { get; set; }
        public decimal AlphaVsQqq { get; set; }
    }
}