using Alpaca.Markets;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdated { get; set; }

    public bool IsActive { get; set; }
    public bool IsTradingPaused { get; set; }

    // Navigation properties
    public virtual ICollection<Position> Positions { get; set; } = new List<Position>();
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}