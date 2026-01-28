using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TradingSystem.Functions.Models;

public class Position
{
    [Key]
    public int PositionId { get; set; }

    [Required]
    public int PortfolioId { get; set; }

    [Required]
    [MaxLength(10)]
    public string Symbol { get; set; } = string.Empty;

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal AverageCostBasis { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal CurrentPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnrealizedProfitLoss { get; set; }

    [Column(TypeName = "decimal(10,4)")]
    public decimal UnrealizedProfitLossPercent { get; set; }

    public DateTime OpenedAt { get; set; }
    public DateTime LastUpdated { get; set; }

    // Navigation property
    [ForeignKey("PortfolioId")]
    public virtual Portfolio Portfolio { get; set; } = null!;
}