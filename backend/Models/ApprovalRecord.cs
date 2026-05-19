using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PurchaseApproval.Utils;

namespace PurchaseApproval.Models;

/// <summary>
/// 审批记录实体
/// </summary>
public class ApprovalRecord
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>采购申请ID</summary>
    [Required]
    public Guid RequestId { get; set; }

    /// <summary>审批人ID（原审批人，如存在委托则为委托人ID）</summary>
    [Required]
    public Guid ApproverId { get; set; }

    /// <summary>审批操作</summary>
    [Required]
    public ApprovalAction Action { get; set; }

    /// <summary>审批意见</summary>
    [MaxLength(500)]
    public string Comment { get; set; } = string.Empty;

    /// <summary>审批级别 (1=经理, 2=财务, 3=总经理)</summary>
    public int ApprovalLevel { get; set; }

    /// <summary>实际操作人ID（用于委托场景，记录代为审批的用户；直接审批时与 ApproverId 相同）</summary>
    public Guid OperatorId { get; set; }

    /// <summary>是否为委托操作（操作人非原审批人）</summary>
    public bool IsDelegated { get; set; }

    public DateTime CreatedAt { get; set; } = DateTimeHelper.GetBeijingTime();

    // Navigation properties
    [ForeignKey(nameof(RequestId))]
    public virtual PurchaseRequest? Request { get; set; }

    [ForeignKey(nameof(ApproverId))]
    public virtual User? Approver { get; set; }

    /// <summary>实际操作人导航属性</summary>
    [ForeignKey(nameof(OperatorId))]
    public virtual User? Operator { get; set; }
}
