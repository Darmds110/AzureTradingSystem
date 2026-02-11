using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TradingSystem.Functions.Models;

public class Position
{
    [Key]
    public int PositionId { get; set; }

    public int PortfolioId { get; set; }

    [Required]
    [MaxLength(10)]
    public string Symbol { get; set; } = string.Empty;

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal AverageCostBasis { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CurrentPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal MarketValue { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnrealizedProfitLoss { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnrealizedProfitLossPercent { get; set; }

    /// <summary>
    /// Maps to 'FirstPurchaseDate' column in database
    /// </summary>
    [Column("FirstPurchaseDate")]
    public DateTime OpenedAt { get; set; }

    /// <summary>
    /// Maps to 'LastUpdateDate' column in database
    /// </summary>
    [Column("LastUpdateDate")]
    public DateTime LastUpdated { get; set; }

    public int HoldingPeriodDays { get; set; }

    public bool IsActive { get; set; }

    // Navigation property
    [ForeignKey("PortfolioId")]
    public virtual Portfolio? Portfolio { get; set; }
}