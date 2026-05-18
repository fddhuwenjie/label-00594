using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PurchaseApproval.DTOs;
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
    /// 初始化 DelegationsController 实例
    /// </summary>
    /// <param name="delegationService">委托服务</param>
    /// <param name="userService">用户服务</param>
    public DelegationsController(IDelegationService delegationService, IUserService userService)
    {
        _delegationService = delegationService;
        _userService = userService;
    }

    /// <summary>
    /// 获取当前用户的委托记录（作为委托人和被委托人）
    /// </summary>
    /// <returns>包含 asDelegator 和 asDelegatee 两个列表的匿名对象</returns>
    [HttpGet("my")]
    public async Task<IActionResult> GetMyDelegations()
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "认证信息无效" });
        }

        var asDelegator = await _delegationService.GetDelegationsByDelegatorAsync(userId.Value);
        var asDelegatee = await _delegationService.GetDelegationsByDelegateeAsync(userId.Value);

        return Ok(new
        {
            asDelegator = asDelegator.Select(MapDelegationToResponse),
            asDelegatee = asDelegatee.Select(MapDelegationToResponse)
        });
    }

    /// <summary>
    /// 获取当前用户生效中的委托记录
    /// </summary>
    /// <returns>生效中的委托记录；若无则返回 null</returns>
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveDelegation()
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "认证信息无效" });
        }

        var delegation = await _delegationService.GetActiveDelegationAsync(userId.Value);
        if (delegation == null)
        {
            return Ok(null);
        }

        return Ok(MapDelegationToResponse(delegation));
    }

    /// <summary>
    /// 获取所有委托记录（仅管理员可访问）
    /// </summary>
    /// <returns>所有委托记录列表</returns>
    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllDelegations()
    {
        var delegations = await _delegationService.GetAllDelegationsAsync();
        return Ok(delegations.Select(MapDelegationToResponse));
    }

    /// <summary>
    /// 创建审批委托记录
    /// </summary>
    /// <param name="dto">创建委托的请求数据</param>
    /// <returns>创建成功的委托记录；若违反业务约束则返回 400</returns>
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
            userId.Value,
            dto.DelegateeId,
            dto.StartDate,
            dto.EndDate,
            dto.Reason);

        if (delegation == null)
        {
            return BadRequest(new { message = "创建委托失败，请检查：1.同一时间段只能有一个生效委托；2.委托不可传递；3.被委托人必须是审批角色" });
        }

        return Ok(MapDelegationToResponse(delegation));
    }

    /// <summary>
    /// 撤销指定的委托记录
    /// </summary>
    /// <param name="id">委托记录ID</param>
    /// <returns>撤销成功返回 200；失败返回 400</returns>
    [HttpPut("{id}/revoke")]
    public async Task<IActionResult> Revoke(Guid id)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "认证信息无效" });
        }

        var result = await _delegationService.RevokeDelegationAsync(id, userId.Value);
        if (!result)
        {
            return BadRequest(new { message = "撤销委托失败，请检查委托状态或权限" });
        }

        return Ok(new { message = "已撤销委托" });
    }

    /// <summary>
    /// 获取可委托的审批人列表（排除自己和普通员工、管理员）
    /// </summary>
    /// <returns>可委托的审批人列表</returns>
    [HttpGet("approvers")]
    public async Task<IActionResult> GetAvailableApprovers()
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "认证信息无效" });
        }

        var currentUser = await _userService.GetByIdAsync(userId.Value);
        if (currentUser == null)
        {
            return Unauthorized(new { message = "用户不存在" });
        }

        var allUsers = await _userService.GetAllAsync();
        var approvers = allUsers
            .Where(u => u.Id != userId.Value
                     && u.Role != UserRole.Employee
                     && u.Role != UserRole.Admin)
            .Select(u => new
            {
                id = u.Id,
                displayName = u.DisplayName,
                role = u.Role.ToString(),
                department = u.Department
            });

        return Ok(approvers);
    }

    /// <summary>
    /// 将 Delegation 实体映射为 API 响应匿名对象
    /// </summary>
    /// <param name="d">委托实体</param>
    /// <returns>包含委托详情的匿名对象</returns>
    private static object MapDelegationToResponse(Delegation d)
    {
        return new
        {
            id = d.Id,
            delegatorId = d.DelegatorId,
            delegatorName = d.Delegator?.DisplayName,
            delegateeId = d.DelegateeId,
            delegateeName = d.Delegatee?.DisplayName,
            startDate = d.StartDate,
            endDate = d.EndDate,
            reason = d.Reason,
            status = d.Status.ToString(),
            statusValue = (int)d.Status,
            createdAt = d.CreatedAt,
            updatedAt = d.UpdatedAt
        };
    }
}
