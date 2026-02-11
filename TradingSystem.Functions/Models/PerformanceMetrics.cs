using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TradingSystem.Functions.Models;

public class PerformanceMetrics
{
    [Key]
    public int MetricId { get; set; }

    public int PortfolioId { get; set; }

    public DateTime MetricDate { get; set; }

    [Required]
    [MaxLength(20)]
    public string PeriodType { get; set; } = "DAILY"; // DAILY, WEEKLY, MONTHLY

    [Column(TypeName = "decimal(18,2)")]
    public decimal PortfolioValue { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CashBalance { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PositionsValue { get; set; }

    /// <summary>
    /// Maps to 'DailyReturn' column in database
    /// </summary>
    [Column("DailyReturn", TypeName = "decimal(18,4)")]
    public decimal PeriodReturnPercent { get; set; }

    /// <summary>
    /// Maps to 'CumulativeReturn' column in database
    /// </summary>
    [Column("CumulativeReturn", TypeName = "decimal(18,4)")]
    public decimal TotalReturnPercent { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PeakValue { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Drawdown { get; set; }

    /// <summary>
    /// Maps to 'MaxDrawdown' column in database
    /// </summary>
    [Column("MaxDrawdown", TypeName = "decimal(18,4)")]
    public decimal MaxDrawdownPercent { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? SharpeRatio { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? WinRate { get; set; }

    public int TotalTrades { get; set; }

    public int WinningTrades { get; set; }

    public int LosingTrades { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? AverageWin { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? AverageLoss { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? AverageHoldingPeriod { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? AzureCostsMTD { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? NetROI { get; set; }

    [Column("SPY_Return", TypeName = "decimal(18,4)")]
    public decimal? SPYReturn { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation property
    [ForeignKey("PortfolioId")]
    public virtual Portfolio? Portfolio { get; set; }
}