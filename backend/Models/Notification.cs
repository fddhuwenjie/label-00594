using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PurchaseApproval.Utils;

namespace PurchaseApproval.Models;

/// <summary>
/// 通知实体
/// </summary>
public class Notification
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>接收用户ID</summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>通知标题</summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>通知内容</summary>
    [MaxLength(1000)]
    public string Content { get; set; } = string.Empty;

    /// <summary>关联申请ID (可选)</summary>
    public Guid? RequestId { get; set; }

    /// <summary>是否已读</summary>
    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTimeHelper.GetBeijingTime();

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }

    [ForeignKey(nameof(RequestId))]
    public virtual PurchaseRequest? Request { get; set; }
}
