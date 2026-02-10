using System;

namespace TradingSystem.Functions.Models
{
    /// <summary>
    /// Represents account information retrieved from Alpaca
    /// </summary>
    public class AccountInfo
    {
        /// <summary>
        /// Total equity (cash + market value of positions)
        /// </summary>
        public decimal Equity { get; set; }

        /// <summary>
        /// Available cash balance
        /// </summary>
        public decimal Cash { get; set; }

        /// <summary>
        /// Available buying power
        /// </summary>
        public decimal BuyingPower { get; set; }

        /// <summary>
        /// Account status (ACTIVE, ACCOUNT_UPDATED, etc.)
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Alpaca account number
        /// </summary>
        public string AccountNumber { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when this data was retrieved
        /// </summary>
        public DateTime RetrievedAt { get; set; }
    }
}