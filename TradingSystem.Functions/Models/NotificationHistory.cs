using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TradingSystem.Functions.Models;

public class NotificationHistory
{
    [Key]
    public int NotificationId { get; set; }

    public DateTime SentAt { get; set; }

    [Required]
    [MaxLength(50)]
    public string NotificationType { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Priority { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Column(TypeName = "nvarchar(max)")]
    public string Body { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Recipient { get; set; } = string.Empty;

    public bool WasSuccessful { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? ErrorMessage { get; set; }
}