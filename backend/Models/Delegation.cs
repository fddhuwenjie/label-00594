using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PurchaseApproval.Utils;

namespace PurchaseApproval.Models;

/// <summary>
/// 审批委托实体
/// </summary>
public class Delegation
{
    /// <summary>
    /// 委托ID
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// 原审批人ID
    /// </summary>
    [Required]
    public Guid GrantorId { get; set; }

    /// <summary>
    /// 被委托人ID
    /// </summary>
    [Required]
    public Guid TrusteeId { get; set; }

    /// <summary>
    /// 委托开始日期
    /// </summary>
    [Required]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// 委托结束日期
    /// </summary>
    [Required]
    public DateTime EndDate { get; set; }

    /// <summary>
    /// 委托原因
    /// </summary>
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// 是否生效
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTimeHelper.GetBeijingTime();

    // Navigation properties

    /// <summary>
    /// 原审批人
    /// </summary>
    [ForeignKey(nameof(GrantorId))]
    public virtual User? Grantor { get; set; }

    /// <summary>
    /// 被委托人
    /// </summary>
    [ForeignKey(nameof(TrusteeId))]
    public virtual User? Trustee { get; set; }
}
