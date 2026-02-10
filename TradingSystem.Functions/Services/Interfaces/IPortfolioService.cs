using TradingSystem.Functions.Models;

namespace TradingSystem.Functions.Services.Interfaces
{
    /// <summary>
    /// Service for managing portfolio state and positions
    /// </summary>
    public interface IPortfolioService
    {
        /// <summary>
        /// Gets the current active portfolio
        /// </summary>
        Task<Portfolio?> GetCurrentPortfolioAsync();

        /// <summary>
        /// Syncs portfolio state with broker account data (2-parameter version for backward compatibility)
        /// </summary>
        Task SyncPortfolioStateAsync(AccountInfo accountInfo, List<PositionInfo> positions);

        /// <summary>
        /// Syncs portfolio state with broker account data (3-parameter version)
        /// </summary>
        Task SyncPortfolioStateAsync(int portfolioId, AccountInfo accountInfo, List<PositionInfo> positions);

        /// <summary>
        /// Halts trading for a portfolio (1-parameter version - uses current portfolio)
        /// </summary>
        Task HaltTradingAsync(string reason);

        /// <summary>
        /// Halts trading for a specific portfolio (2-parameter version)
        /// </summary>
        Task HaltTradingAsync(int portfolioId, string reason);

        /// <summary>
        /// Resumes trading for a portfolio
        /// </summary>
        Task ResumeTradingAsync(int portfolioId);

        /// <summary>
        /// Updates portfolio drawdown and checks risk limits
        /// </summary>
        Task<DrawdownStatus> UpdateDrawdownAsync(int portfolioId);

        /// <summary>
        /// Updates holding periods for all open positions
        /// </summary>
        Task UpdateHoldingPeriodsAsync();

        /// <summary>
        /// Gets all current positions for a portfolio
        /// </summary>
        Task<List<Position>> GetPositionsAsync(int portfolioId);
    }

    /// <summary>
    /// Drawdown status for risk monitoring
    /// </summary>
    public class DrawdownStatus
    {
        public decimal CurrentDrawdownPercent { get; set; }
        public decimal PeakValue { get; set; }
        public decimal CurrentValue { get; set; }
        public bool ShouldHalt { get; set; }
        public bool ShouldWarn { get; set; }
    }
}