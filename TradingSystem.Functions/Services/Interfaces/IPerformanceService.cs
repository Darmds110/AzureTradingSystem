using TradingSystem.Functions.Models;

namespace TradingSystem.Functions.Services.Interfaces
{
    /// <summary>
    /// Service for calculating and storing performance metrics
    /// </summary>
    public interface IPerformanceService
    {
        /// <summary>
        /// Calculates and stores daily performance metrics
        /// </summary>
        /// <param name="portfolioId">Portfolio ID</param>
        /// <returns>Daily performance metrics</returns>
        Task<PerformanceMetrics> CalculateDailyMetricsAsync(int portfolioId);

        /// <summary>
        /// Calculates and stores weekly performance metrics
        /// </summary>
        /// <param name="portfolioId">Portfolio ID</param>
        /// <returns>Weekly performance metrics</returns>
        Task<PerformanceMetrics> CalculateWeeklyMetricsAsync(int portfolioId);

        /// <summary>
        /// Calculates and stores monthly performance metrics
        /// </summary>
        /// <param name="portfolioId">Portfolio ID</param>
        /// <returns>Monthly performance metrics</returns>
        Task<PerformanceMetrics> CalculateMonthlyMetricsAsync(int portfolioId);

        /// <summary>
        /// Compares portfolio performance against benchmarks (SPY, QQQ)
        /// </summary>
        /// <param name="portfolioId">Portfolio ID</param>
        /// <param name="asOfDate">Date to calculate comparison for</param>
        /// <returns>Benchmark comparison results</returns>
        Task<BenchmarkComparison> CompareToBenchmarksAsync(int portfolioId, DateTime asOfDate);

        /// <summary>
        /// Calculates performance metrics for each trading strategy
        /// </summary>
        /// <param name="portfolioId">Portfolio ID</param>
        Task CalculateStrategyPerformanceAsync(int portfolioId);

        /// <summary>
        /// Calculates Sharpe ratio for a given period
        /// </summary>
        /// <param name="portfolioId">Portfolio ID</param>
        /// <param name="startDate">Period start date</param>
        /// <param name="endDate">Period end date</param>
        /// <returns>Sharpe ratio</returns>
        Task<decimal> CalculateSharpeRatioAsync(int portfolioId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Calculates maximum drawdown for a given period
        /// </summary>
        /// <param name="portfolioId">Portfolio ID</param>
        /// <param name="startDate">Period start date</param>
        /// <param name="endDate">Period end date</param>
        /// <returns>Maximum drawdown percentage (negative number)</returns>
        Task<decimal> CalculateMaxDrawdownAsync(int portfolioId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Gets trade statistics (win rate, avg gain, avg loss, etc.)
        /// </summary>
        /// <param name="portfolioId">Portfolio ID</param>
        /// <param name="startDate">Period start date</param>
        /// <param name="endDate">Period end date</param>
        /// <returns>Trade statistics</returns>
        Task<TradeStatistics> GetTradeStatisticsAsync(int portfolioId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Gets the latest performance metrics for a portfolio
        /// </summary>
        /// <param name="portfolioId">Portfolio ID</param>
        /// <returns>Latest performance metrics</returns>
        Task<PerformanceMetrics?> GetLatestMetricsAsync(int portfolioId);

        /// <summary>
        /// Gets historical performance metrics for charting
        /// </summary>
        /// <param name="portfolioId">Portfolio ID</param>
        /// <param name="periodType">Period type (Daily, Weekly, Monthly)</param>
        /// <param name="count">Number of periods to retrieve</param>
        /// <returns>List of historical metrics</returns>
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
        public decimal AlphaVsSpy { get; set; }  // Portfolio return - SPY return
        public decimal AlphaVsQqq { get; set; }  // Portfolio return - QQQ return
        public DateTime AsOfDate { get; set; }
    }

    /// <summary>
    /// Trade statistics for analysis
    /// </summary>
    public class TradeStatistics
    {
        public int TotalTrades { get; set; }
        public int WinningTrades { get; set; }
        public int LosingTrades { get; set; }
        public decimal WinRatePercent { get; set; }
        public decimal AverageGain { get; set; }
        public decimal AverageLoss { get; set; }
        public decimal LargestGain { get; set; }
        public decimal LargestLoss { get; set; }
        public decimal ProfitFactor { get; set; }  // Total gains / Total losses
        public decimal AverageHoldingPeriodDays { get; set; }
        public decimal ExpectedValue { get; set; }  // (WinRate * AvgGain) - (LossRate * AvgLoss)
    }
}