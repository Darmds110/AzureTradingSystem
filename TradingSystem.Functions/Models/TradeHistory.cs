using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TradingSystem.Functions.Models;

public class TradeHistory
{
    [Key]
    public int TradeId { get; set; }

    [Required]
    public int PortfolioId { get; set; }

    public int? StrategyId { get; set; }

    public int? BuyOrderId { get; set; }
    public int? SellOrderId { get; set; }

    [Required]
    [MaxLength(10)]
    public string Symbol { get; set; } = string.Empty;

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal EntryPrice { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ExitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal RealizedProfitLoss { get; set; }

    [Column(TypeName = "decimal(10,4)")]
    public decimal RealizedProfitLossPercent { get; set; }

    public DateTime EntryDate { get; set; }
    public DateTime ExitDate { get; set; }

    public int HoldingPeriodDays { get; set; }

    public DateTime CreatedAt { get; set; }
}