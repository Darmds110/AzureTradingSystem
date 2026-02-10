using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TradingSystem.Functions.Models;

public class Portfolio
{
    [Key]
    public int PortfolioId { get; set; }

    [Required]
    [MaxLength(100)]
    public string PortfolioName { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal InitialCapital { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CurrentCash { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CurrentEquity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal BuyingPower { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PeakValue { get; set; }

    /// <summary>
    /// Current drawdown from peak as a percentage (negative number)
    /// Example: -15.5 means 15.5% below peak
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal CurrentDrawdownPercent { get; set; }

    /// <summary>
    /// Reason why trading was paused (if applicable)
    /// </summary>
    [MaxLength(500)]
    public string? PausedReason { get; set; }

    /// <summary>
    /// Last time portfolio was synced with Alpaca
    /// </summary>
    public DateTime? LastSyncTimestamp { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Maps to 'UpdatedAt' column in database
    /// </summary>
    [Column("UpdatedAt")]
    public DateTime LastUpdated { get; set; }

    public bool IsActive { get; set; }
    public bool IsTradingPaused { get; set; }

    // Navigation properties
    public virtual ICollection<Position> Positions { get; set; } = new List<Position>();
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}