using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TradingSystem.Functions.Models;

public class StrategyConfiguration
{
    [Key]
    public int StrategyId { get; set; }

    [Required]
    [MaxLength(100)]
    public string StrategyName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string StrategyType { get; set; } = string.Empty;

    [Column(TypeName = "nvarchar(max)")]
    public string? Description { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? EntryRulesJson { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? ExitRulesJson { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal MaxPositionSizePercent { get; set; }

    public int MaxConcurrentPositions { get; set; }

    public bool IsActive { get; set; }

    public int Version { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime LastModified { get; set; }

    // Navigation property
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}