using TradingSystem.Functions.Models;

namespace TradingSystem.Functions.Services.Interfaces
{
    /// <summary>
    /// Service for managing portfolio state in the database
    /// </summary>
    public interface IPortfolioService
    {
        /// <summary>
        /// Syncs portfolio state with Alpaca account data
        /// Updates portfolio equity, cash, positions, etc.
        /// </summary>
        /// <param name="accountInfo">Current account information from Alpaca</param>
        /// <param name="positions">Current positions from Alpaca</param>
        Task SyncPortfolioStateAsync(AccountInfo accountInfo, List<PositionInfo> positions);

        /// <summary>
        /// Gets the current portfolio from database
        /// </summary>
        /// <returns>Current portfolio</returns>
        Task<Portfolio> GetCurrentPortfolioAsync();

        /// <summary>
        /// Updates portfolio value and related metrics
        /// </summary>
        /// <param name="newValue">New portfolio value</param>
        Task UpdatePortfolioValueAsync(decimal newValue);

        /// <summary>
        /// Updates peak value if current value is higher
        /// </summary>
        /// <param name="currentValue">Current portfolio value</param>
        Task UpdatePeakValueAsync(decimal currentValue);

        /// <summary>
        /// Halts trading by setting IsTradingPaused flag
        /// Used when risk limits are exceeded
        /// </summary>
        /// <param name="reason">Reason for halting (e.g., "20% drawdown exceeded")</param>
        Task HaltTradingAsync(string reason);

        /// <summary>
        /// Resumes trading by clearing IsTradingPaused flag
        /// Requires manual intervention after trading halt
        /// </summary>
        Task ResumeTradingAsync();

        /// <summary>
        /// Updates current drawdown percentage
        /// </summary>
        /// <param name="drawdownPercent">Current drawdown (negative number)</param>
        Task UpdateDrawdownAsync(decimal drawdownPercent);

        /// <summary>
        /// Calculates holding period for all positions
        /// </summary>
        Task UpdateHoldingPeriodsAsync();
    }
}