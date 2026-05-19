using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PurchaseApproval.Extensions;
using PurchaseApproval.Models;
using PurchaseApproval.Services;

namespace PurchaseApproval.Controllers;

/// <summary>
/// 审批委托管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DelegationsController : ControllerBase
{
    private readonly IDelegationService _delegationService;
    private readonly IUserService _userService;

    /// <summary>
    /// 初始化 DelegationsController
    /// </summary>
    /// <param name="delegationService">委托服务</param>
    /// <param name="userService">用户服务</param>
    public DelegationsController(
        IDelegationService delegationService,
        IUserService userService)
    {
        _delegationService = delegationService;
        _userService = userService;
    }

    /// <summary>
    /// 创建审批委托
    /// </summary>
    /// <param name="dto">委托创建DTO</param>
    /// <returns>创建的委托记录</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDelegationDto dto)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "认证信息无效" });
        }

        var delegation = await _delegationService.CreateDelegationAsync(
            delegatorId: userId.Value,
            delegateId: dto.DelegateId,
            startDate: dto.StartDate,
            endDate: dto.EndDate,
            remark: dto.Remark);

        if (delegation == null)
        {
            return BadRequest(new { message = "创建委托失败，请检查委托人角色、被委托人、时间范围等约束条件" });
        }

        return Ok(new
        {
            id = delegation.Id,
            delegatorId = delegation.DelegatorId,
            delegateId = delegation.DelegateId,
            startDate = delegation.StartDate,
            endDate = delegation.EndDate,
            remark = delegation.Remark,
            createdAt = delegation.CreatedAt
        });
    }

    /// <summary>
    /// 撤销审批委托
    /// </summary>
    /// <param name="id">委托记录ID</param>
    /// <returns>撤销结果</returns>
    [HttpPost("{id}/revoke")]
    public async Task<IActionResult> Revoke(Guid id)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "认证信息无效" });
        }

        var success = await _delegationService.RevokeDelegationAsync(id, userId.Value);
        if (!success)
        {
            return BadRequest(new { message = "撤销委托失败，可能原因：委托不存在、已撤销或无权操作" });
        }

        return Ok(new { message = "委托已撤销" });
    }

    /// <summary>
    /// 获取当前用户作为委托人的所有委托记录
    /// </summary>
    /// <returns>委托记录列表</returns>
    [HttpGet("as-delegator")]
    public async Task<IActionResult> GetAsDelegator()
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "认证信息无效" });
        }

        var delegations = await _delegationService.GetDelegationsAsDelegatorAsync(userId.Value);
        return Ok(delegations.Select(MapToDto));
    }

    /// <summary>
    /// 获取当前用户作为被委托人的所有委托记录
    /// </summary>
    /// <returns>委托记录列表</returns>
    [HttpGet("as-delegate")]
    public async Task<IActionResult> GetAsDelegate()
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "认证信息无效" });
        }

        var delegations = await _delegationService.GetDelegationsAsDelegateAsync(userId.Value);
        return Ok(delegations.Select(MapToDto));
    }

    /// <summary>
    /// 获取当前用户当前生效的委托（作为被委托人）
    /// </summary>
    /// <returns>生效的委托记录，若无则返回null</returns>
    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "认证信息无效" });
        }

        var delegation = await _delegationService.GetActiveDelegationAsync(userId.Value);
        return Ok(delegation == null ? null : MapToDto(delegation));
    }

    /// <summary>
    /// 获取所有委托记录（仅管理员）
    /// </summary>
    /// <returns>所有委托记录列表</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userRole = User.GetUserRole();
        if (userRole != UserRole.Admin)
        {
            return Forbid("仅管理员可查看所有委托记录");
        }

        var delegations = await _delegationService.GetAllDelegationsAsync();
        return Ok(delegations.Select(MapToDto));
    }

    /// <summary>
    /// 获取可供选择的被委托人列表
    /// </summary>
    /// <returns>用户列表</returns>
    [HttpGet("available-delegates")]
    public async Task<IActionResult> GetAvailableDelegates()
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "认证信息无效" });
        }

        var users = await _userService.GetAllAsync();
        var delegates = users
            .Where(u => u.Id != userId.Value && u.Role != UserRole.Admin)
            .Select(u => new
            {
                id = u.Id,
                username = u.Username,
                displayName = u.DisplayName,
                role = u.Role.ToString(),
                roleValue = (int)u.Role,
                department = u.Department
            })
            .ToList();

        return Ok(delegates);
    }

    /// <summary>
    /// 将委托实体映射为DTO
    /// </summary>
    /// <param name="d">委托实体</param>
    /// <returns>委托DTO</returns>
    private static object MapToDto(Delegation d)
    {
        return new
        {
            id = d.Id,
            delegatorId = d.DelegatorId,
            delegatorName = d.Delegator?.DisplayName,
            delegateId = d.DelegateId,
            delegateName = d.Delegate?.DisplayName,
            startDate = d.StartDate,
            endDate = d.EndDate,
            remark = d.Remark,
            createdAt = d.CreatedAt,
            isRevoked = d.IsRevoked,
            revokedAt = d.RevokedAt
        };
    }
}

/// <summary>
/// 创建审批委托DTO
/// </summary>
public class CreateDelegationDto
{
    /// <summary>被委托人ID</summary>
    public Guid DelegateId { get; set; }

    /// <summary>委托开始时间</summary>
    public DateTime StartDate { get; set; }

    /// <summary>委托结束时间</summary>
    public DateTime EndDate { get; set; }

    /// <summary>委托备注</summary>
    public string? Remark { get; set; }
}
