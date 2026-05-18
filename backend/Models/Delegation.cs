using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PurchaseApproval.Utils;

namespace PurchaseApproval.Models;

/// <summary>
/// 审批委托实体
/// </summary>
public class Delegation
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>委托人ID（原审批人）</summary>
    [Required]
    public Guid DelegatorId { get; set; }

    /// <summary>被委托人ID（代为审批的人）</summary>
    [Required]
    public Guid DelegateeId { get; set; }

    /// <summary>委托开始日期</summary>
    [Required]
    public DateTime StartDate { get; set; }

    /// <summary>委托结束日期</summary>
    [Required]
    public DateTime EndDate { get; set; }

    /// <summary>委托原因</summary>
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>委托状态</summary>
    public DelegationStatus Status { get; set; } = DelegationStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTimeHelper.GetBeijingTime();

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey(nameof(DelegatorId))]
    public virtual User? Delegator { get; set; }

    [ForeignKey(nameof(DelegateeId))]
    public virtual User? Delegatee { get; set; }
}

/// <summary>
/// 委托状态
/// </summary>
public enum DelegationStatus
{
    /// <summary>生效中</summary>
    Active = 0,
    /// <summary>已撤销</summary>
    Revoked = 1,
    /// <summary>已过期</summary>
    Expired = 2
}
