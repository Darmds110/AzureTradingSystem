namespace TradingSystem.Functions.Models
{
    /// <summary>
    /// Represents portfolio drawdown information
    /// </summary>
    public class DrawdownInfo
    {
        /// <summary>
        /// Current portfolio value
        /// </summary>
        public decimal CurrentValue { get; set; }

        /// <summary>
        /// Peak portfolio value (all-time high)
        /// </summary>
        public decimal PeakValue { get; set; }

        /// <summary>
        /// Drawdown percentage (negative number)
        /// Calculation: ((CurrentValue - PeakValue) / PeakValue) * 100
        /// </summary>
        public decimal Percentage { get; set; }

        /// <summary>
        /// Drawdown in dollar amount
        /// </summary>
        public decimal DollarAmount { get; set; }

        /// <summary>
        /// Date when peak value was reached
        /// </summary>
        public DateTime PeakDate { get; set; }

        /// <summary>
        /// Number of days since peak
        /// </summary>
        public int DaysSincePeak { get; set; }

        /// <summary>
        /// Whether this drawdown triggers a warning (>= 15%)
        /// </summary>
        public bool IsWarningLevel => Percentage <= -15m;

        /// <summary>
        /// Whether this drawdown triggers trading halt (>= 20%)
        /// </summary>
        public bool IsCriticalLevel => Percentage <= -20m;

        /// <summary>
        /// Severity level: OK, WARNING, CRITICAL
        /// </summary>
        public string Severity
        {
            get
            {
                if (IsCriticalLevel) return "CRITICAL";
                if (IsWarningLevel) return "WARNING";
                return "OK";
            }
        }

        /// <summary>
        /// Timestamp when this drawdown was calculated
        /// </summary>
        public DateTime CalculatedAt { get; set; }
    }
}