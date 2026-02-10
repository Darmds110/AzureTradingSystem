namespace TradingSystem.Functions.Models
{
    /// <summary>
    /// Position information from broker
    /// </summary>
    public class PositionInfo
    {
        public string Symbol { get; set; } = string.Empty;
        public decimal Quantity { get; set; }  // decimal to match Alpaca SDK
        public decimal AverageCostBasis { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal MarketValue { get; set; }
        public decimal UnrealizedPL { get; set; }
        public decimal UnrealizedPLPercent { get; set; }
        public string Side { get; set; } = string.Empty;
        public string AssetId { get; set; } = string.Empty;
    }
}