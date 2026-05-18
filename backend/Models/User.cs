using System.ComponentModel.DataAnnotations;
using PurchaseApproval.Utils;

namespace PurchaseApproval.Models;

/// <summary>
/// 用户实体
/// </summary>
public class User
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; }

    [MaxLength(100)]
    public string Department { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTimeHelper.GetBeijingTime();

    // Navigation properties
    public virtual ICollection<PurchaseRequest> PurchaseRequests { get; set; } = new List<PurchaseRequest>();
    public virtual ICollection<ApprovalRecord> ApprovalRecords { get; set; } = new List<ApprovalRecord>();
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public virtual ICollection<Delegation> GrantedDelegations { get; set; } = new List<Delegation>();
    public virtual ICollection<Delegation> ReceivedDelegations { get; set; } = new List<Delegation>();
}
