using System;
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

    [MaxLength(50)]
    public string? StrategyType { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// JSON configuration containing entry and exit rules
    /// Maps to 'ConfigurationJson' column in database
    /// </summary>
    [Column("ConfigurationJson")]
    public string? EntryRulesJson { get; set; }

    /// <summary>
    /// For backward compatibility - not stored separately in DB
    /// </summary>
    [NotMapped]
    public string? ExitRulesJson { get; set; }

    public bool IsActive { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal MaxPositionSizePercent { get; set; }

    public int MaxConcurrentPositions { get; set; }

    public int Version { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Maps to 'UpdatedAt' column in database
    /// </summary>
    [Column("UpdatedAt")]
    public DateTime? LastModified { get; set; }

    [MaxLength(100)]
    public string? CreatedBy { get; set; }
}