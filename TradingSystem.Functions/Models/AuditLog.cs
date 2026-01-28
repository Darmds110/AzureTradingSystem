using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TradingSystem.Functions.Models;

public class AuditLog
{
    [Key]
    public int LogId { get; set; }

    public DateTime Timestamp { get; set; }

    [Required]
    [MaxLength(50)]
    public string EventType { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Severity { get; set; } = string.Empty;

    public int? PortfolioId { get; set; }
    public int? StrategyId { get; set; }
    public int? OrderId { get; set; }

    [MaxLength(10)]
    public string? Symbol { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? Message { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? AdditionalDataJson { get; set; }
}