using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PurchaseApproval.Utils;

namespace PurchaseApproval.Models;

/// <summary>
/// 审批委托实体：审批人将自己的审批权限临时委托给另一个用户
/// </summary>
public class Delegation
{
    /// <summary>委托记录唯一标识</summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>委托人ID（原审批人）</summary>
    [Required]
    public Guid DelegatorId { get; set; }

    /// <summary>被委托人ID（代为审批人）</summary>
    [Required]
    public Guid DelegateId { get; set; }

    /// <summary>委托开始时间</summary>
    [Required]
    public DateTime StartDate { get; set; }

    /// <summary>委托结束时间</summary>
    [Required]
    public DateTime EndDate { get; set; }

    /// <summary>委托创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTimeHelper.GetBeijingTime();

    /// <summary>是否已被撤销</summary>
    public bool IsRevoked { get; set; } = false;

    /// <summary>撤销时间</summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>委托备注</summary>
    [MaxLength(500)]
    public string? Remark { get; set; }

    // Navigation properties

    /// <summary>委托人导航属性</summary>
    [ForeignKey(nameof(DelegatorId))]
    public virtual User? Delegator { get; set; }

    /// <summary>被委托人导航属性</summary>
    [ForeignKey(nameof(DelegateId))]
    public virtual User? Delegate { get; set; }
}
