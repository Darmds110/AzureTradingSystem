namespace TradingSystem.Functions.Models
{
    /// <summary>
    /// Trade statistics for a given period
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
        public decimal ProfitFactor { get; set; }
        public decimal AverageHoldingPeriodDays { get; set; }
        public decimal ExpectedValue { get; set; }
    }
}