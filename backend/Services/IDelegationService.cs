using PurchaseApproval.Models;

namespace PurchaseApproval.Services;

/// <summary>
/// 审批委托服务接口
/// </summary>
public interface IDelegationService
{
    /// <summary>
    /// 创建审批委托
    /// </summary>
    /// <param name="delegatorId">委托人ID（原审批人）</param>
    /// <param name="delegateId">被委托人ID</param>
    /// <param name="startDate">委托开始时间</param>
    /// <param name="endDate">委托结束时间</param>
    /// <param name="remark">委托备注</param>
    /// <returns>创建成功返回委托记录，失败返回null</returns>
    Task<Delegation?> CreateDelegationAsync(Guid delegatorId, Guid delegateId, DateTime startDate, DateTime endDate, string? remark);

    /// <summary>
    /// 撤销审批委托
    /// </summary>
    /// <param name="delegationId">委托记录ID</param>
    /// <param name="operatorId">操作人ID</param>
    /// <returns>撤销是否成功</returns>
    Task<bool> RevokeDelegationAsync(Guid delegationId, Guid operatorId);

    /// <summary>
    /// 获取指定用户的所有委托记录（作为委托人）
    /// </summary>
    /// <param name="delegatorId">委托人ID</param>
    /// <returns>委托记录列表</returns>
    Task<List<Delegation>> GetDelegationsAsDelegatorAsync(Guid delegatorId);

    /// <summary>
    /// 获取指定用户的所有委托记录（作为被委托人）
    /// </summary>
    /// <param name="delegateId">被委托人ID</param>
    /// <returns>委托记录列表</returns>
    Task<List<Delegation>> GetDelegationsAsDelegateAsync(Guid delegateId);

    /// <summary>
    /// 获取指定用户当前生效的委托（作为被委托人）
    /// </summary>
    /// <param name="delegateId">被委托人ID</param>
    /// <param name="checkTime">检查时间（默认当前时间）</param>
    /// <returns>生效中的委托记录，若无则返回null</returns>
    Task<Delegation?> GetActiveDelegationAsync(Guid delegateId, DateTime? checkTime = null);

    /// <summary>
    /// 检查用户是否拥有指定审批权限（包括委托代理）
    /// </summary>
    /// <param name="operatorId">操作人ID</param>
    /// <param name="requiredRole">所需审批角色</param>
    /// <returns>包含审批人信息和是否委托代理的结果</returns>
    Task<(bool IsAuthorized, Guid ApproverId, bool IsDelegated)> CheckApprovalAuthorityAsync(Guid operatorId, UserRole requiredRole);

    /// <summary>
    /// 获取所有委托记录（管理员使用）
    /// </summary>
    /// <returns>所有委托记录列表</returns>
    Task<List<Delegation>> GetAllDelegationsAsync();
}
