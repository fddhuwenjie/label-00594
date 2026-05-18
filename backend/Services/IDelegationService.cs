using PurchaseApproval.Models;

namespace PurchaseApproval.Services;

/// <summary>
/// 审批委托服务接口
/// </summary>
public interface IDelegationService
{
    /// <summary>
    /// 获取指定用户发起的所有委托记录
    /// </summary>
    /// <param name="grantorId">原审批人ID</param>
    /// <returns>委托记录列表</returns>
    Task<List<Delegation>> GetGrantedDelegationsAsync(Guid grantorId);

    /// <summary>
    /// 获取指定用户作为被委托人的所有委托记录
    /// </summary>
    /// <param name="trusteeId">被委托人ID</param>
    /// <returns>委托记录列表</returns>
    Task<List<Delegation>> GetReceivedDelegationsAsync(Guid trusteeId);

    /// <summary>
    /// 根据ID获取委托记录
    /// </summary>
    /// <param name="id">委托ID</param>
    /// <returns>委托记录，不存在则返回null</returns>
    Task<Delegation?> GetByIdAsync(Guid id);

    /// <summary>
    /// 创建新的委托记录
    /// </summary>
    /// <param name="grantorId">原审批人ID</param>
    /// <param name="trusteeId">被委托人ID</param>
    /// <param name="startDate">委托开始日期</param>
    /// <param name="endDate">委托结束日期</param>
    /// <param name="reason">委托原因</param>
    /// <returns>创建的委托记录</returns>
    Task<Delegation> CreateAsync(Guid grantorId, Guid trusteeId, DateTime startDate, DateTime endDate, string? reason);

    /// <summary>
    /// 更新委托记录
    /// </summary>
    /// <param name="id">委托ID</param>
    /// <param name="trusteeId">被委托人ID</param>
    /// <param name="startDate">委托开始日期</param>
    /// <param name="endDate">委托结束日期</param>
    /// <param name="reason">委托原因</param>
    /// <param name="isActive">是否生效</param>
    /// <returns>更新后的委托记录，失败返回null</returns>
    Task<Delegation?> UpdateAsync(Guid id, Guid trusteeId, DateTime startDate, DateTime endDate, string? reason, bool isActive);

    /// <summary>
    /// 删除委托记录
    /// </summary>
    /// <param name="id">委托ID</param>
    /// <param name="grantorId">原审批人ID（用于权限验证）</param>
    /// <returns>是否删除成功</returns>
    Task<bool> DeleteAsync(Guid id, Guid grantorId);

    /// <summary>
    /// 检查指定用户在指定时间段内是否有有效的委托记录
    /// </summary>
    /// <param name="grantorId">原审批人ID</param>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <param name="excludeId">需要排除的委托ID（用于更新时的检查）</param>
    /// <returns>是否存在冲突的委托</returns>
    Task<bool> HasActiveDelegationAsync(Guid grantorId, DateTime startDate, DateTime endDate, Guid? excludeId = null);

    /// <summary>
    /// 获取用户当前有效的委托人（即该用户被委托的原审批人）
    /// </summary>
    /// <param name="trusteeId">被委托人ID</param>
    /// <returns>当前有效的委托记录列表</returns>
    Task<List<Delegation>> GetActiveGrantorsAsync(Guid trusteeId);

    /// <summary>
    /// 获取用户当前有效的被委托人
    /// </summary>
    /// <param name="grantorId">原审批人ID</param>
    /// <returns>当前有效的委托记录，不存在则返回null</returns>
    Task<Delegation?> GetActiveTrusteeAsync(Guid grantorId);

    /// <summary>
    /// 检查用户是否是某个审批人的被委托人
    /// </summary>
    /// <param name="trusteeId">被委托人ID</param>
    /// <param name="grantorId">原审批人ID</param>
    /// <returns>是否存在有效的委托关系</returns>
    Task<bool> IsTrusteeForAsync(Guid trusteeId, Guid grantorId);
}
