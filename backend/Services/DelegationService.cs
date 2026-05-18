using Microsoft.EntityFrameworkCore;
using PurchaseApproval.Data;
using PurchaseApproval.Models;
using PurchaseApproval.Utils;

namespace PurchaseApproval.Services;

/// <summary>
/// 审批委托服务实现
/// </summary>
public class DelegationService : IDelegationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<DelegationService> _logger;

    /// <summary>
    /// 初始化审批委托服务
    /// </summary>
    /// <param name="context">数据库上下文</param>
    /// <param name="logger">日志记录器</param>
    public DelegationService(AppDbContext context, ILogger<DelegationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取指定用户发起的所有委托记录
    /// </summary>
    /// <param name="grantorId">原审批人ID</param>
    /// <returns>委托记录列表</returns>
    public async Task<List<Delegation>> GetGrantedDelegationsAsync(Guid grantorId)
    {
        return await _context.Delegations
            .Include(d => d.Trustee)
            .Where(d => d.GrantorId == grantorId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// 获取指定用户作为被委托人的所有委托记录
    /// </summary>
    /// <param name="trusteeId">被委托人ID</param>
    /// <returns>委托记录列表</returns>
    public async Task<List<Delegation>> GetReceivedDelegationsAsync(Guid trusteeId)
    {
        return await _context.Delegations
            .Include(d => d.Grantor)
            .Where(d => d.TrusteeId == trusteeId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// 根据ID获取委托记录
    /// </summary>
    /// <param name="id">委托ID</param>
    /// <returns>委托记录，不存在则返回null</returns>
    public async Task<Delegation?> GetByIdAsync(Guid id)
    {
        return await _context.Delegations
            .Include(d => d.Grantor)
            .Include(d => d.Trustee)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    /// <summary>
    /// 创建新的委托记录
    /// </summary>
    /// <param name="grantorId">原审批人ID</param>
    /// <param name="trusteeId">被委托人ID</param>
    /// <param name="startDate">委托开始日期</param>
    /// <param name="endDate">委托结束日期</param>
    /// <param name="reason">委托原因</param>
    /// <returns>创建的委托记录</returns>
    public async Task<Delegation> CreateAsync(Guid grantorId, Guid trusteeId, DateTime startDate, DateTime endDate, string? reason)
    {
        var delegation = new Delegation
        {
            Id = Guid.NewGuid(),
            GrantorId = grantorId,
            TrusteeId = trusteeId,
            StartDate = startDate,
            EndDate = endDate,
            Reason = reason?.Trim() ?? string.Empty,
            IsActive = true,
            CreatedAt = DateTimeHelper.GetBeijingTime()
        };

        _context.Delegations.Add(delegation);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "创建审批委托: delegationId={DelegationId}, grantorId={GrantorId}, trusteeId={TrusteeId}, startDate={StartDate}, endDate={EndDate}",
            delegation.Id,
            grantorId,
            trusteeId,
            startDate,
            endDate);

        return delegation;
    }

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
    public async Task<Delegation?> UpdateAsync(Guid id, Guid trusteeId, DateTime startDate, DateTime endDate, string? reason, bool isActive)
    {
        var delegation = await _context.Delegations.FindAsync(id);
        if (delegation == null)
        {
            return null;
        }

        delegation.TrusteeId = trusteeId;
        delegation.StartDate = startDate;
        delegation.EndDate = endDate;
        delegation.Reason = reason?.Trim() ?? string.Empty;
        delegation.IsActive = isActive;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "更新审批委托: delegationId={DelegationId}, isActive={IsActive}",
            id,
            isActive);

        return delegation;
    }

    /// <summary>
    /// 删除委托记录
    /// </summary>
    /// <param name="id">委托ID</param>
    /// <param name="grantorId">原审批人ID（用于权限验证）</param>
    /// <returns>是否删除成功</returns>
    public async Task<bool> DeleteAsync(Guid id, Guid grantorId)
    {
        var delegation = await _context.Delegations.FindAsync(id);
        if (delegation == null || delegation.GrantorId != grantorId)
        {
            return false;
        }

        _context.Delegations.Remove(delegation);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "删除审批委托: delegationId={DelegationId}",
            id);

        return true;
    }

    /// <summary>
    /// 检查指定用户在指定时间段内是否有有效的委托记录
    /// </summary>
    /// <param name="grantorId">原审批人ID</param>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <param name="excludeId">需要排除的委托ID（用于更新时的检查）</param>
    /// <returns>是否存在冲突的委托</returns>
    public async Task<bool> HasActiveDelegationAsync(Guid grantorId, DateTime startDate, DateTime endDate, Guid? excludeId = null)
    {
        var query = _context.Delegations
            .Where(d => d.GrantorId == grantorId && d.IsActive);

        if (excludeId.HasValue)
        {
            query = query.Where(d => d.Id != excludeId.Value);
        }

        var existing = await query.ToListAsync();

        return existing.Any(d =>
            (d.StartDate <= endDate && d.EndDate >= startDate));
    }

    /// <summary>
    /// 获取用户当前有效的委托人（即该用户被委托的原审批人）
    /// </summary>
    /// <param name="trusteeId">被委托人ID</param>
    /// <returns>当前有效的委托记录列表</returns>
    public async Task<List<Delegation>> GetActiveGrantorsAsync(Guid trusteeId)
    {
        var now = DateTimeHelper.GetBeijingTime();
        return await _context.Delegations
            .Include(d => d.Grantor)
            .Where(d =>
                d.TrusteeId == trusteeId &&
                d.IsActive &&
                d.StartDate <= now &&
                d.EndDate >= now)
            .ToListAsync();
    }

    /// <summary>
    /// 获取用户当前有效的被委托人
    /// </summary>
    /// <param name="grantorId">原审批人ID</param>
    /// <returns>当前有效的委托记录，不存在则返回null</returns>
    public async Task<Delegation?> GetActiveTrusteeAsync(Guid grantorId)
    {
        var now = DateTimeHelper.GetBeijingTime();
        return await _context.Delegations
            .Include(d => d.Trustee)
            .FirstOrDefaultAsync(d =>
                d.GrantorId == grantorId &&
                d.IsActive &&
                d.StartDate <= now &&
                d.EndDate >= now);
    }

    /// <summary>
    /// 检查用户是否是某个审批人的被委托人
    /// </summary>
    /// <param name="trusteeId">被委托人ID</param>
    /// <param name="grantorId">原审批人ID</param>
    /// <returns>是否存在有效的委托关系</returns>
    public async Task<bool> IsTrusteeForAsync(Guid trusteeId, Guid grantorId)
    {
        var now = DateTimeHelper.GetBeijingTime();
        return await _context.Delegations.AnyAsync(d =>
            d.TrusteeId == trusteeId &&
            d.GrantorId == grantorId &&
            d.IsActive &&
            d.StartDate <= now &&
            d.EndDate >= now);
    }
}
