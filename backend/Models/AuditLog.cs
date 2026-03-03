using System.ComponentModel.DataAnnotations;
using PurchaseApproval.Utils;

namespace PurchaseApproval.Models;

public class AuditLog
{
    [Key]
    public Guid Id { get; set; }

    public Guid? ActorId { get; set; }

    [MaxLength(50)]
    public string? ActorUsername { get; set; }

    [MaxLength(30)]
    public string? ActorRole { get; set; }

    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ResourceType { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ResourceId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Result { get; set; } = "Success";

    [MaxLength(2000)]
    public string? Details { get; set; }

    [MaxLength(100)]
    public string? CorrelationId { get; set; }

    [MaxLength(64)]
    public string? IpAddress { get; set; }

    [MaxLength(512)]
    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTimeHelper.GetBeijingTime();
}
