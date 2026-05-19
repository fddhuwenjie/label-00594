using Microsoft.EntityFrameworkCore;
using PurchaseApproval.Data;
using PurchaseApproval.Models;
using PurchaseApproval.Utils;

namespace PurchaseApproval.Services;

/// <summary>
/// 审批委托服务实现：处理审批权限的委托、撤销、查询等业务逻辑
/// </summary>
public class DelegationService : IDelegationService
{
    private readonly AppDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<DelegationService> _logger;

    /// <summary>
    /// 初始化 DelegationService
    /// </summary>
    /// <param name="context">数据库上下文</param>
    /// <param name="auditLogService">审计日志服务</param>
    /// <param name="logger">日志器</param>
    public DelegationService(
        AppDbContext context,
        IAuditLogService auditLogService,
        ILogger<DelegationService> logger)
    {
        _context = context;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// 创建审批委托
    /// </summary>
    /// <param name="delegatorId">委托人ID（原审批人）</param>
    /// <param name="delegateId">被委托人ID</param>
    /// <param name="startDate">委托开始时间</param>
    /// <param name="endDate">委托结束时间</param>
    /// <param name="remark">委托备注</param>
    /// <returns>创建成功返回委托记录，失败返回null</returns>
    /// <remarks>
    /// 业务约束：
    /// 1. 委托人和被委托人必须存在
    /// 2. 委托人必须拥有审批角色（经理/财务/总经理）
    /// 3. 同一时间段内一个审批人只能有一个生效的委托记录
    /// 4. 委托不可传递：被委托人不能再委托给第三人
    /// 5. 被委托人必须是普通员工或其他审批角色
    /// 6. 开始时间必须早于结束时间
    /// 7. 开始时间不能早于当前时间
    /// </remarks>
    public async Task<Delegation?> CreateDelegationAsync(Guid delegatorId, Guid delegateId, DateTime startDate, DateTime endDate, string? remark)
    {
        var now = DateTimeHelper.GetBeijingTime();

        if (delegatorId == delegateId)
        {
            _logger.LogWarning("创建委托失败：委托人和被委托人不能是同一用户");
            return null;
        }

        if (startDate >= endDate)
        {
            _logger.LogWarning("创建委托失败：开始时间必须早于结束时间");
            return null;
        }

        if (startDate < now)
        {
            _logger.LogWarning("创建委托失败：开始时间不能早于当前时间");
            return null;
        }

        var delegator = await _context.Users.FindAsync(delegatorId);
        if (delegator == null)
        {
            _logger.LogWarning("创建委托失败：委托人不存在，delegatorId={DelegatorId}", delegatorId);
            return null;
        }

        var delegateUser = await _context.Users.FindAsync(delegateId);
        if (delegateUser == null)
        {
            _logger.LogWarning("创建委托失败：被委托人不存在，delegateId={DelegateId}", delegateId);
            return null;
        }

        if (delegator.Role != UserRole.Manager && delegator.Role != UserRole.Finance && delegator.Role != UserRole.Director)
        {
            _logger.LogWarning("创建委托失败：委托人必须拥有审批角色，delegatorRole={Role}", delegator.Role);
            return null;
        }

        if (delegateUser.Role == UserRole.Admin)
        {
            _logger.LogWarning("创建委托失败：不能将权限委托给系统管理员");
            return null;
        }

        var existingActive = await _context.Delegations
            .FirstOrDefaultAsync(d =>
                d.DelegatorId == delegatorId &&
                !d.IsRevoked &&
                d.StartDate <= now &&
                d.EndDate >= now);

        if (existingActive != null)
        {
            _logger.LogWarning("创建委托失败：该审批人在当前时间段已有生效的委托记录，delegatorId={DelegatorId}", delegatorId);
            return null;
        }

        var hasOverlap = await _context.Delegations
            .AnyAsync(d =>
                d.DelegatorId == delegatorId &&
                !d.IsRevoked &&
                d.StartDate <= endDate &&
                d.EndDate >= startDate);

        if (hasOverlap)
        {
            _logger.LogWarning("创建委托失败：该审批人在委托时间段内已有重叠的委托记录，delegatorId={DelegatorId}", delegatorId);
            return null;
        }

        var activeAsDelegate = await _context.Delegations
            .FirstOrDefaultAsync(d =>
                d.DelegateId == delegateId &&
                !d.IsRevoked &&
                d.StartDate <= now &&
                d.EndDate >= now);

        if (activeAsDelegate != null)
        {
            _logger.LogWarning("创建委托失败：被委托人已有生效的委托，委托不可传递，delegateId={DelegateId}", delegateId);
            return null;
        }

        var delegation = new Delegation
        {
            Id = Guid.NewGuid(),
            DelegatorId = delegatorId,
            DelegateId = delegateId,
            StartDate = startDate,
            EndDate = endDate,
            Remark = remark,
            CreatedAt = now
        };

        _context.Delegations.Add(delegation);
        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            action: "Delegation.Create",
            resourceType: "Delegation",
            resourceId: delegation.Id.ToString(),
            result: "Success",
            details: $"delegator={delegator.Username};delegate={delegateUser.Username};startDate={startDate:yyyy-MM-dd};endDate={endDate:yyyy-MM-dd}",
            actorId: delegatorId,
            actorUsername: delegator.Username,
            actorRole: delegator.Role.ToString());

        _logger.LogInformation(
            "创建委托成功：delegationId={DelegationId}, delegatorId={DelegatorId}, delegateId={DelegateId}",
            delegation.Id, delegatorId, delegateId);

        return delegation;
    }

    /// <summary>
    /// 撤销审批委托
    /// </summary>
    /// <param name="delegationId">委托记录ID</param>
    /// <param name="operatorId">操作人ID</param>
    /// <returns>撤销是否成功</returns>
    /// <remarks>
    /// 只有委托人本人或管理员可以撤销委托
    /// </remarks>
    public async Task<bool> RevokeDelegationAsync(Guid delegationId, Guid operatorId)
    {
        var delegation = await _context.Delegations.FindAsync(delegationId);
        if (delegation == null)
        {
            _logger.LogWarning("撤销委托失败：委托记录不存在，delegationId={DelegationId}", delegationId);
            return false;
        }

        var @operator = await _context.Users.FindAsync(operatorId);
        if (@operator == null)
        {
            _logger.LogWarning("撤销委托失败：操作人不存在，operatorId={OperatorId}", operatorId);
            return false;
        }

        var isDelegator = delegation.DelegatorId == operatorId;
        var isAdmin = @operator.Role == UserRole.Admin;

        if (!isDelegator && !isAdmin)
        {
            _logger.LogWarning("撤销委托失败：无权撤销该委托，operatorId={OperatorId}", operatorId);
            return false;
        }

        if (delegation.IsRevoked)
        {
            _logger.LogWarning("撤销委托失败：该委托已被撤销，delegationId={DelegationId}", delegationId);
            return false;
        }

        delegation.IsRevoked = true;
        delegation.RevokedAt = DateTimeHelper.GetBeijingTime();

        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            action: "Delegation.Revoke",
            resourceType: "Delegation",
            resourceId: delegationId.ToString(),
            result: "Success",
            details: $"isDelegator={isDelegator};isAdmin={isAdmin}",
            actorId: operatorId,
            actorUsername: @operator.Username,
            actorRole: @operator.Role.ToString());

        _logger.LogInformation(
            "撤销委托成功：delegationId={DelegationId}, operatorId={OperatorId}",
            delegationId, operatorId);

        return true;
    }

    /// <summary>
    /// 获取指定用户的所有委托记录（作为委托人）
    /// </summary>
    /// <param name="delegatorId">委托人ID</param>
    /// <returns>委托记录列表</returns>
    public async Task<List<Delegation>> GetDelegationsAsDelegatorAsync(Guid delegatorId)
    {
        return await _context.Delegations
            .Include(d => d.Delegate)
            .Where(d => d.DelegatorId == delegatorId)
            .OrderByDescending(d => d.StartDate)
            .ToListAsync();
    }

    /// <summary>
    /// 获取指定用户的所有委托记录（作为被委托人）
    /// </summary>
    /// <param name="delegateId">被委托人ID</param>
    /// <returns>委托记录列表</returns>
    public async Task<List<Delegation>> GetDelegationsAsDelegateAsync(Guid delegateId)
    {
        return await _context.Delegations
            .Include(d => d.Delegator)
            .Where(d => d.DelegateId == delegateId)
            .OrderByDescending(d => d.StartDate)
            .ToListAsync();
    }

    /// <summary>
    /// 获取指定用户当前生效的委托（作为被委托人）
    /// </summary>
    /// <param name="delegateId">被委托人ID</param>
    /// <param name="checkTime">检查时间（默认当前时间）</param>
    /// <returns>生效中的委托记录，若无则返回null</returns>
    public async Task<Delegation?> GetActiveDelegationAsync(Guid delegateId, DateTime? checkTime = null)
    {
        var time = checkTime ?? DateTimeHelper.GetBeijingTime();
        return await _context.Delegations
            .Include(d => d.Delegator)
            .FirstOrDefaultAsync(d =>
                d.DelegateId == delegateId &&
                !d.IsRevoked &&
                d.StartDate <= time &&
                d.EndDate >= time);
    }

    /// <summary>
    /// 检查用户是否拥有指定审批权限（包括委托代理）
    /// </summary>
    /// <param name="operatorId">操作人ID</param>
    /// <param name="requiredRole">所需审批角色</param>
    /// <returns>包含审批人信息和是否委托代理的结果</returns>
    /// <remarks>
    /// 优先检查操作人是否直接拥有该角色权限；
    /// 若无，再检查是否拥有该角色的生效委托。
    /// </remarks>
    public async Task<(bool IsAuthorized, Guid ApproverId, bool IsDelegated)> CheckApprovalAuthorityAsync(Guid operatorId, UserRole requiredRole)
    {
        var @operator = await _context.Users.FindAsync(operatorId);
        if (@operator == null)
        {
            return (false, Guid.Empty, false);
        }

        if (@operator.Role == requiredRole)
        {
            return (true, operatorId, false);
        }

        var now = DateTimeHelper.GetBeijingTime();
        var activeDelegation = await _context.Delegations
            .Include(d => d.Delegator)
            .FirstOrDefaultAsync(d =>
                d.DelegateId == operatorId &&
                !d.IsRevoked &&
                d.StartDate <= now &&
                d.EndDate >= now &&
                d.Delegator != null &&
                d.Delegator!.Role == requiredRole);

        if (activeDelegation != null)
        {
            return (true, activeDelegation.DelegatorId, true);
        }

        return (false, Guid.Empty, false);
    }

    /// <summary>
    /// 获取所有委托记录（管理员使用）
    /// </summary>
    /// <returns>所有委托记录列表</returns>
    public async Task<List<Delegation>> GetAllDelegationsAsync()
    {
        return await _context.Delegations
            .Include(d => d.Delegator)
            .Include(d => d.Delegate)
            .OrderByDescending(d => d.StartDate)
            .ToListAsync();
    }
}
