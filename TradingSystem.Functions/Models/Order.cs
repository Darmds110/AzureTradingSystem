using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TradingSystem.Functions.Models;

public class Order
{
    [Key]
    public int OrderId { get; set; }

    [Required]
    public int PortfolioId { get; set; }

    public int? StrategyId { get; set; }

    [Required]
    [MaxLength(10)]
    public string Symbol { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string OrderType { get; set; } = string.Empty; // MARKET, LIMIT, STOP, TRAILING_STOP

    [Required]
    [MaxLength(10)]
    public string Side { get; set; } = string.Empty; // BUY, SELL

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? LimitPrice { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? StopPrice { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = string.Empty; // PENDING, SUBMITTED, FILLED, CANCELED, REJECTED

    [MaxLength(100)]
    public string? AlpacaOrderId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? FilledPrice { get; set; }

    public int? FilledQuantity { get; set; }

    public DateTime SubmittedAt { get; set; }
    public DateTime? FilledAt { get; set; }
    public DateTime? CanceledAt { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? ReasonJson { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    [ForeignKey("PortfolioId")]
    public virtual Portfolio Portfolio { get; set; } = null!;

    [ForeignKey("StrategyId")]
    public virtual StrategyConfiguration? Strategy { get; set; }
}