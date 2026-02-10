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
        Task<DailyMetrics> CalculateDailyMetricsAsync(int portfolioId);

        /// <summary>
        /// Calculates weekly performance metrics
        /// </summary>
        Task<PeriodMetrics> CalculateWeeklyMetricsAsync(int portfolioId);

        /// <summary>
        /// Calculates monthly performance metrics
        /// </summary>
        Task<PeriodMetrics> CalculateMonthlyMetricsAsync(int portfolioId);

        /// <summary>
        /// Compares portfolio performance to benchmarks (SPY, QQQ)
        /// </summary>
        Task<BenchmarkComparison> CompareToBenchmarksAsync(int portfolioId, DateTime date);

        /// <summary>
        /// Calculates performance metrics per strategy
        /// </summary>
        Task CalculateStrategyPerformanceAsync(int portfolioId);
    }

    /// <summary>
    /// Daily performance metrics
    /// </summary>
    public class DailyMetrics
    {
        public decimal DailyReturnPercent { get; set; }
        public decimal TotalReturnPercent { get; set; }
        public decimal PortfolioValue { get; set; }
        public DateTime Date { get; set; }
    }

    /// <summary>
    /// Period performance metrics (weekly/monthly)
    /// </summary>
    public class PeriodMetrics
    {
        public decimal PeriodReturnPercent { get; set; }
        public decimal WinRatePercent { get; set; }
        public decimal SharpeRatio { get; set; }
        public decimal MaxDrawdownPercent { get; set; }
        public int TotalTrades { get; set; }
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