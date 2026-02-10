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
        /// Syncs portfolio state with broker account data
        /// </summary>
        Task SyncPortfolioStateAsync(int portfolioId, AccountInfo accountInfo, List<PositionInfo> positions);

        /// <summary>
        /// Halts trading for a portfolio
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
        /// Gets all current positions for a portfolio
        /// </summary>
        Task<List<Position>> GetPositionsAsync(int portfolioId);
    }
}