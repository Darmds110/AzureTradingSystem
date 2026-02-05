namespace TradingSystem.Functions.Models
{
    /// <summary>
    /// Represents a position (holding) in the portfolio from Alpaca
    /// </summary>
    public class PositionInfo
    {
        /// <summary>
        /// Stock symbol (e.g., AAPL, MSFT)
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Number of shares held (can be fractional if enabled)
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Average price paid per share (cost basis)
        /// </summary>
        public decimal AverageCostBasis { get; set; }

        /// <summary>
        /// Current market price per share
        /// </summary>
        public decimal CurrentPrice { get; set; }

        /// <summary>
        /// Total market value of position (quantity * current price)
        /// </summary>
        public decimal MarketValue { get; set; }

        /// <summary>
        /// Unrealized profit/loss in dollars
        /// </summary>
        public decimal UnrealizedPL { get; set; }

        /// <summary>
        /// Unrealized profit/loss as percentage
        /// </summary>
        public decimal UnrealizedPLPercent { get; set; }

        /// <summary>
        /// Side of the position (long or short)
        /// </summary>
        public string Side { get; set; } = "long";

        /// <summary>
        /// Asset ID from Alpaca
        /// </summary>
        public string AssetId { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when position was opened
        /// </summary>
        public DateTime? EntryDate { get; set; }
    }
}