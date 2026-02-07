using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PurchaseApproval.Models;

/// <summary>
/// 采购申请实体
/// </summary>
public class PurchaseRequest
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>申请编号 (自动生成)</summary>
    [Required]
    [MaxLength(20)]
    public string RequestNumber { get; set; } = string.Empty;

    /// <summary>申请人ID</summary>
    [Required]
    public Guid ApplicantId { get; set; }

    /// <summary>物品名称</summary>
    [Required]
    [MaxLength(200)]
    public string ItemName { get; set; } = string.Empty;

    /// <summary>采购数量</summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    /// <summary>单价</summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    /// <summary>总金额 (计算字段)</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount => Quantity * UnitPrice;

    /// <summary>采购原因</summary>
    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>紧急程度</summary>
    public Urgency Urgency { get; set; } = Urgency.Normal;

    /// <summary>申请状态</summary>
    public RequestStatus Status { get; set; } = RequestStatus.Draft;

    /// <summary>工作流实例ID</summary>
    [MaxLength(100)]
    public string? WorkflowId { get; set; }

    /// <summary>当前审批级别 (1=经理, 2=财务, 3=总经理)</summary>
    public int CurrentApprovalLevel { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(ApplicantId))]
    public virtual User? Applicant { get; set; }

    public virtual ICollection<ApprovalRecord> ApprovalRecords { get; set; } = new List<ApprovalRecord>();
}
