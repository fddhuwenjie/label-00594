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

    /// <summary>审批人ID</summary>
    [Required]
    public Guid ApproverId { get; set; }

    /// <summary>实际操作人ID（委托审批时为被委托人，否则与审批人相同）</summary>
    public Guid? ActualOperatorId { get; set; }

    /// <summary>审批操作</summary>
    [Required]
    public ApprovalAction Action { get; set; }

    /// <summary>审批意见</summary>
    [MaxLength(500)]
    public string Comment { get; set; } = string.Empty;

    /// <summary>审批级别 (1=经理, 2=财务, 3=总经理)</summary>
    public int ApprovalLevel { get; set; }

    public DateTime CreatedAt { get; set; } = DateTimeHelper.GetBeijingTime();

    // Navigation properties
    [ForeignKey(nameof(RequestId))]
    public virtual PurchaseRequest? Request { get; set; }

    [ForeignKey(nameof(ApproverId))]
    public virtual User? Approver { get; set; }

    [ForeignKey(nameof(ActualOperatorId))]
    public virtual User? ActualOperator { get; set; }
}
