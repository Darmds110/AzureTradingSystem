using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TradingSystem.Functions.Models;

public class TradeHistory
{
    [Key]
    public int TradeId { get; set; }

    public int PortfolioId { get; set; }

    [Required]
    [MaxLength(10)]
    public string Symbol { get; set; } = string.Empty;

    public int? StrategyId { get; set; }

    /// <summary>
    /// Maps to 'EntryOrderId' column in database
    /// </summary>
    [Column("EntryOrderId")]
    public int? BuyOrderId { get; set; }

    /// <summary>
    /// Maps to 'ExitOrderId' column in database
    /// </summary>
    [Column("ExitOrderId")]
    public int? SellOrderId { get; set; }

    public DateTime EntryDate { get; set; }

    public DateTime? ExitDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal EntryPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? ExitPrice { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? EntryValue { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? ExitValue { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? RealizedProfitLoss { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? RealizedProfitLossPercent { get; set; }

    public int? HoldingPeriodDays { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Commission { get; set; }

    public bool? IsWinner { get; set; }

    [MaxLength(50)]
    public string? ExitReason { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    [ForeignKey("PortfolioId")]
    public virtual Portfolio? Portfolio { get; set; }

    [ForeignKey("StrategyId")]
    public virtual StrategyConfiguration? Strategy { get; set; }
}