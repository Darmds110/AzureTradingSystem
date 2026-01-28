using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TradingSystem.Functions.Models;

public class PerformanceMetrics
{
    [Key]
    public int MetricId { get; set; }

    [Required]
    public int PortfolioId { get; set; }

    public DateTime MetricDate { get; set; }

    [Required]
    [MaxLength(20)]
    public string PeriodType { get; set; } = string.Empty; // DAILY, WEEKLY, MONTHLY

    [Column(TypeName = "decimal(18,2)")]
    public decimal PortfolioValue { get; set; }

    [Column(TypeName = "decimal(10,4)")]
    public decimal TotalReturnPercent { get; set; }

    [Column(TypeName = "decimal(10,4)")]
    public decimal PeriodReturnPercent { get; set; }

    [Column(TypeName = "decimal(10,4)")]
    public decimal MaxDrawdownPercent { get; set; }

    [Column(TypeName = "decimal(10,4)")]
    public decimal? SharpeRatio { get; set; }

    [Column(TypeName = "decimal(10,4)")]
    public decimal? WinRate { get; set; }

    public int? TotalTrades { get; set; }
    public int? WinningTrades { get; set; }
    public int? LosingTrades { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation property
    [ForeignKey("PortfolioId")]
    public virtual Portfolio Portfolio { get; set; } = null!;
}