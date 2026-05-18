using PurchaseApproval.Models;

namespace PurchaseApproval.Services;

/// <summary>
/// 审批委托服务接口
/// </summary>
public interface IDelegationService
{
    /// <summary>
    /// 创建委托记录
    /// </summary>
    /// <param name="delegatorId">委托人ID</param>
    /// <param name="delegateeId">被委托人ID</param>
    /// <param name="startDate">委托开始日期</param>
    /// <param name="endDate">委托结束日期</param>
    /// <param name="reason">委托原因</param>
    /// <returns>创建的委托记录；若违反业务约束则返回 null</returns>
    Task<Delegation?> CreateDelegationAsync(Guid delegatorId, Guid delegateeId, DateTime startDate, DateTime endDate, string? reason);

    /// <summary>
    /// 撤销委托
    /// </summary>
    /// <param name="delegationId">委托记录ID</param>
    /// <param name="delegatorId">委托人ID（用于权限校验）</param>
    /// <returns>是否撤销成功</returns>
    Task<bool> RevokeDelegationAsync(Guid delegationId, Guid delegatorId);

    /// <summary>
    /// 获取指定用户作为委托人的所有委托记录
    /// </summary>
    /// <param name="delegatorId">委托人ID</param>
    /// <returns>委托记录列表</returns>
    Task<List<Delegation>> GetDelegationsByDelegatorAsync(Guid delegatorId);

    /// <summary>
    /// 获取指定用户作为被委托人的所有委托记录
    /// </summary>
    /// <param name="delegateeId">被委托人ID</param>
    /// <returns>委托记录列表</returns>
    Task<List<Delegation>> GetDelegationsByDelegateeAsync(Guid delegateeId);

    /// <summary>
    /// 获取指定用户当前生效的委托记录（作为委托人）
    /// </summary>
    /// <param name="delegatorId">委托人ID</param>
    /// <returns>生效中的委托记录，若无则返回 null</returns>
    Task<Delegation?> GetActiveDelegationAsync(Guid delegatorId);

    /// <summary>
    /// 检查指定审批人是否已将其权限委托给他人，并返回被委托人ID
    /// </summary>
    /// <param name="approverId">原审批人ID</param>
    /// <returns>被委托人ID；若无生效委托则返回 null</returns>
    Task<Guid?> GetDelegateeForApproverAsync(Guid approverId);

    /// <summary>
    /// 获取所有委托记录（管理员使用）
    /// </summary>
    /// <returns>所有委托记录列表</returns>
    Task<List<Delegation>> GetAllDelegationsAsync();

    /// <summary>
    /// 更新已过期的委托状态
    /// </summary>
    /// <returns>受影响的记录数</returns>
    Task<int> ExpireOutdatedDelegationsAsync();
}
