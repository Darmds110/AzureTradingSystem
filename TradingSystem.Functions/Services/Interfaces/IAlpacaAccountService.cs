using TradingSystem.Functions.Models;

namespace TradingSystem.Functions.Services.Interfaces
{
    /// <summary>
    /// Service for interacting with Alpaca brokerage account API
    /// </summary>
    public interface IAlpacaAccountService
    {
        /// <summary>
        /// Gets current account information (equity, cash, buying power, status)
        /// </summary>
        /// <returns>Account information</returns>
        Task<AccountInfo> GetAccountInfoAsync();

        /// <summary>
        /// Gets all current positions (holdings) in the account
        /// </summary>
        /// <returns>List of positions</returns>
        Task<List<PositionInfo>> GetPositionsAsync();

        /// <summary>
        /// Cancels all pending orders in the account
        /// Used when trading is halted due to risk limits
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> CancelAllOrdersAsync();

        /// <summary>
        /// Gets account activity for a specific date range
        /// Useful for performance tracking and reconciliation
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>List of account activities</returns>
        Task<List<object>> GetAccountActivitiesAsync(DateTime startDate, DateTime endDate);
    }
}