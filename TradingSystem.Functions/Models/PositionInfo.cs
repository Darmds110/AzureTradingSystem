using System;

namespace TradingSystem.Functions.Models
{
    /// <summary>
    /// Represents position information retrieved from Alpaca
    /// </summary>
    public class PositionInfo
    {
        /// <summary>
        /// Stock symbol (e.g., AAPL)
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Number of shares held
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Average cost per share
        /// </summary>
        public decimal AverageCostBasis { get; set; }

        /// <summary>
        /// Current market price per share
        /// </summary>
        public decimal CurrentPrice { get; set; }

        /// <summary>
        /// Total market value of position
        /// </summary>
        public decimal MarketValue { get; set; }

        /// <summary>
        /// Unrealized profit/loss in dollars
        /// </summary>
        public decimal UnrealizedPL { get; set; }

        /// <summary>
        /// Unrealized profit/loss as a decimal (0.05 = 5%)
        /// </summary>
        public decimal UnrealizedPLPercent { get; set; }

        /// <summary>
        /// Position side (long or short)
        /// </summary>
        public string Side { get; set; } = "long";

        /// <summary>
        /// Alpaca asset ID
        /// </summary>
        public string AssetId { get; set; } = string.Empty;
    }
}