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
    /// 初始化 DelegationService 实例
    /// </summary>
    /// <param name="context">数据库上下文</param>
    /// <param name="logger">日志记录器</param>
    public DelegationService(AppDbContext context, ILogger<DelegationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 创建委托记录
    /// </summary>
    /// <param name="delegatorId">委托人ID</param>
    /// <param name="delegateeId">被委托人ID</param>
    /// <param name="startDate">委托开始日期</param>
    /// <param name="endDate">委托结束日期</param>
    /// <param name="reason">委托原因</param>
    /// <returns>创建的委托记录；若违反业务约束则返回 null</returns>
    public async Task<Delegation?> CreateDelegationAsync(Guid delegatorId, Guid delegateeId, DateTime startDate, DateTime endDate, string? reason)
    {
        if (delegatorId == delegateeId)
        {
            _logger.LogWarning("创建委托失败：委托人和被委托人不能是同一人, delegatorId={DelegatorId}", delegatorId);
            return null;
        }

        var delegator = await _context.Users.FindAsync(delegatorId);
        if (delegator == null)
        {
            _logger.LogWarning("创建委托失败：委托人不存在, delegatorId={DelegatorId}", delegatorId);
            return null;
        }

        var delegatee = await _context.Users.FindAsync(delegateeId);
        if (delegatee == null)
        {
            _logger.LogWarning("创建委托失败：被委托人不存在, delegateeId={DelegateeId}", delegateeId);
            return null;
        }

        if (delegator.Role == UserRole.Employee || delegator.Role == UserRole.Admin)
        {
            _logger.LogWarning("创建委托失败：委托人角色无审批权限, delegatorId={DelegatorId}, role={Role}", delegatorId, delegator.Role);
            return null;
        }

        if (delegatee.Role == UserRole.Employee || delegatee.Role == UserRole.Admin)
        {
            _logger.LogWarning("创建委托失败：被委托人角色无审批权限, delegateeId={DelegateeId}, role={Role}", delegateeId, delegatee.Role);
            return null;
        }

        var hasActiveDelegation = await _context.Delegations
            .AnyAsync(d => d.DelegatorId == delegatorId && d.Status == DelegationStatus.Active);
        if (hasActiveDelegation)
        {
            _logger.LogWarning("创建委托失败：委托人已有生效中的委托, delegatorId={DelegatorId}", delegatorId);
            return null;
        }

        var overlappingDelegation = await _context.Delegations
            .AnyAsync(d => d.DelegatorId == delegatorId
                        && d.Status == DelegationStatus.Active
                        && d.StartDate < endDate
                        && d.EndDate > startDate);
        if (overlappingDelegation)
        {
            _logger.LogWarning("创建委托失败：委托人在该时间段内已有委托记录, delegatorId={DelegatorId}", delegatorId);
            return null;
        }

        var isDelegateeAlsoDelegating = await _context.Delegations
            .AnyAsync(d => d.DelegatorId == delegateeId && d.Status == DelegationStatus.Active);
        if (isDelegateeAlsoDelegating)
        {
            _logger.LogWarning("创建委托失败：被委托人已将审批权限委托给他人，委托不可传递, delegateeId={DelegateeId}", delegateeId);
            return null;
        }

        var isDelegateeAlreadyDelegatee = await _context.Delegations
            .AnyAsync(d => d.DelegateeId == delegateeId && d.Status == DelegationStatus.Active);
        if (isDelegateeAlreadyDelegatee)
        {
            _logger.LogWarning("创建委托失败：被委托人已接受其他人的委托，委托不可传递, delegateeId={DelegateeId}", delegateeId);
            return null;
        }

        var delegation = new Delegation
        {
            Id = Guid.NewGuid(),
            DelegatorId = delegatorId,
            DelegateeId = delegateeId,
            StartDate = startDate,
            EndDate = endDate,
            Reason = reason?.Trim() ?? string.Empty,
            Status = DelegationStatus.Active,
            CreatedAt = DateTimeHelper.GetBeijingTime()
        };

        _context.Delegations.Add(delegation);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "创建委托成功: delegatorId={DelegatorId}, delegateeId={DelegateeId}, startDate={StartDate}, endDate={EndDate}",
            delegatorId, delegateeId, startDate, endDate);

        return delegation;
    }

    /// <summary>
    /// 撤销委托
    /// </summary>
    /// <param name="delegationId">委托记录ID</param>
    /// <param name="delegatorId">委托人ID（用于权限校验）</param>
    /// <returns>是否撤销成功</returns>
    public async Task<bool> RevokeDelegationAsync(Guid delegationId, Guid delegatorId)
    {
        var delegation = await _context.Delegations.FindAsync(delegationId);
        if (delegation == null)
        {
            _logger.LogWarning("撤销委托失败：委托记录不存在, delegationId={DelegationId}", delegationId);
            return false;
        }

        if (delegation.DelegatorId != delegatorId)
        {
            _logger.LogWarning("撤销委托失败：无权撤销他人委托, delegationId={DelegationId}, delegatorId={DelegatorId}", delegationId, delegatorId);
            return false;
        }

        if (delegation.Status != DelegationStatus.Active)
        {
            _logger.LogWarning("撤销委托失败：委托状态不是生效中, delegationId={DelegationId}, status={Status}", delegationId, delegation.Status);
            return false;
        }

        delegation.Status = DelegationStatus.Revoked;
        delegation.UpdatedAt = DateTimeHelper.GetBeijingTime();

        await _context.SaveChangesAsync();

        _logger.LogInformation("撤销委托成功: delegationId={DelegationId}", delegationId);
        return true;
    }

    /// <summary>
    /// 获取指定用户作为委托人的所有委托记录
    /// </summary>
    /// <param name="delegatorId">委托人ID</param>
    /// <returns>委托记录列表</returns>
    public async Task<List<Delegation>> GetDelegationsByDelegatorAsync(Guid delegatorId)
    {
        return await _context.Delegations
            .Include(d => d.Delegatee)
            .Where(d => d.DelegatorId == delegatorId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// 获取指定用户作为被委托人的所有委托记录
    /// </summary>
    /// <param name="delegateeId">被委托人ID</param>
    /// <returns>委托记录列表</returns>
    public async Task<List<Delegation>> GetDelegationsByDelegateeAsync(Guid delegateeId)
    {
        return await _context.Delegations
            .Include(d => d.Delegator)
            .Where(d => d.DelegateeId == delegateeId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// 获取指定用户当前生效的委托记录（作为委托人）
    /// </summary>
    /// <param name="delegatorId">委托人ID</param>
    /// <returns>生效中的委托记录，若无则返回 null</returns>
    public async Task<Delegation?> GetActiveDelegationAsync(Guid delegatorId)
    {
        var now = DateTimeHelper.GetBeijingTime();
        return await _context.Delegations
            .Include(d => d.Delegatee)
            .FirstOrDefaultAsync(d =>
                d.DelegatorId == delegatorId &&
                d.Status == DelegationStatus.Active &&
                d.StartDate <= now &&
                d.EndDate >= now);
    }

    /// <summary>
    /// 检查指定审批人是否已将其权限委托给他人，并返回被委托人ID
    /// </summary>
    /// <param name="approverId">原审批人ID</param>
    /// <returns>被委托人ID；若无生效委托则返回 null</returns>
    public async Task<Guid?> GetDelegateeForApproverAsync(Guid approverId)
    {
        var now = DateTimeHelper.GetBeijingTime();
        var delegation = await _context.Delegations
            .FirstOrDefaultAsync(d =>
                d.DelegatorId == approverId &&
                d.Status == DelegationStatus.Active &&
                d.StartDate <= now &&
                d.EndDate >= now);

        return delegation?.DelegateeId;
    }

    /// <summary>
    /// 获取所有委托记录（管理员使用）
    /// </summary>
    /// <returns>所有委托记录列表</returns>
    public async Task<List<Delegation>> GetAllDelegationsAsync()
    {
        return await _context.Delegations
            .Include(d => d.Delegator)
            .Include(d => d.Delegatee)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// 更新已过期的委托状态
    /// </summary>
    /// <returns>受影响的记录数</returns>
    public async Task<int> ExpireOutdatedDelegationsAsync()
    {
        var now = DateTimeHelper.GetBeijingTime();
        var outdatedDelegations = await _context.Delegations
            .Where(d => d.Status == DelegationStatus.Active && d.EndDate < now)
            .ToListAsync();

        foreach (var delegation in outdatedDelegations)
        {
            delegation.Status = DelegationStatus.Expired;
            delegation.UpdatedAt = now;
        }

        if (outdatedDelegations.Count > 0)
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("已过期 {Count} 条委托记录", outdatedDelegations.Count);
        }

        return outdatedDelegations.Count;
    }
}
