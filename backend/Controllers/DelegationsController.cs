using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PurchaseApproval.DTOs;
using PurchaseApproval.Extensions;
using PurchaseApproval.Models;
using PurchaseApproval.Services;

namespace PurchaseApproval.Controllers;

/// <summary>
/// 审批委托控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DelegationsController : ControllerBase
{
    private readonly IDelegationService _delegationService;
    private readonly IUserService _userService;

    /// <summary>
    /// 初始化审批委托控制器
    /// </summary>
    /// <param name="delegationService">委托服务</param>
    /// <param name="userService">用户服务</param>
    public DelegationsController(IDelegationService delegationService, IUserService userService)
    {
        _delegationService = delegationService;
        _userService = userService;
    }

    /// <summary>
    /// 获取当前用户发起的委托记录
    /// </summary>
    /// <returns>委托记录列表</returns>
    [HttpGet("granted")]
    public async Task<IActionResult> GetGranted()
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "认证信息无效" });
        }

        var delegations = await _delegationService.GetGrantedDelegationsAsync(userId.Value);
        return Ok(delegations.Select(MapToResponse));
    }

    /// <summary>
    /// 获取当前用户作为被委托人的委托记录
    /// </summary>
    /// <returns>委托记录列表</returns>
    [HttpGet("received")]
    public async Task<IActionResult> GetReceived()
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "认证信息无效" });
        }

        var delegations = await _delegationService.GetReceivedDelegationsAsync(userId.Value);
        return Ok(delegations.Select(MapToResponse));
    }

    /// <summary>
    /// 根据ID获取委托记录
    /// </summary>
    /// <param name="id">委托ID</param>
    /// <returns>委托记录详情</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "认证信息无效" });
        }

        var delegation = await _delegationService.GetByIdAsync(id);
        if (delegation == null)
        {
            return NotFound(new { message = "委托记录不存在" });
        }

        if (delegation.GrantorId != userId.Value && delegation.TrusteeId != userId.Value)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "无权查看该委托记录" });
        }

        return Ok(MapToResponse(delegation));
    }

    /// <summary>
    /// 创建新的委托记录
    /// </summary>
    /// <param name="dto">创建委托的DTO</param>
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

        var grantor = await _userService.GetByIdAsync(userId.Value);
        if (grantor == null || (grantor.Role != UserRole.Manager && grantor.Role != UserRole.Finance && grantor.Role != UserRole.Director))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "只有审批角色才能创建委托" });
        }

        if (dto.TrusteeId == userId.Value)
        {
            return BadRequest(new { message = "不能委托给自己" });
        }

        var trustee = await _userService.GetByIdAsync(dto.TrusteeId);
        if (trustee == null)
        {
            return BadRequest(new { message = "被委托人不存在" });
        }

        var hasConflict = await _delegationService.HasActiveDelegationAsync(userId.Value, dto.StartDate, dto.EndDate);
        if (hasConflict)
        {
            return BadRequest(new { message = "该时间段内已有有效的委托记录" });
        }

        var delegation = await _delegationService.CreateAsync(
            userId.Value,
            dto.TrusteeId,
            dto.StartDate,
            dto.EndDate,
            dto.Reason);

        return CreatedAtAction(nameof(GetById), new { id = delegation.Id }, MapToResponse(delegation));
    }

    /// <summary>
    /// 更新委托记录
    /// </summary>
    /// <param name="id">委托ID</param>
    /// <param name="dto">更新委托的DTO</param>
    /// <returns>更新后的委托记录</returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDelegationDto dto)
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

        var existing = await _delegationService.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound(new { message = "委托记录不存在" });
        }

        if (existing.GrantorId != userId.Value)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "无权修改该委托记录" });
        }

        if (dto.TrusteeId == userId.Value)
        {
            return BadRequest(new { message = "不能委托给自己" });
        }

        var trustee = await _userService.GetByIdAsync(dto.TrusteeId);
        if (trustee == null)
        {
            return BadRequest(new { message = "被委托人不存在" });
        }

        var hasConflict = await _delegationService.HasActiveDelegationAsync(userId.Value, dto.StartDate, dto.EndDate, id);
        if (hasConflict)
        {
            return BadRequest(new { message = "该时间段内已有有效的委托记录" });
        }

        var updated = await _delegationService.UpdateAsync(
            id,
            dto.TrusteeId,
            dto.StartDate,
            dto.EndDate,
            dto.Reason,
            dto.IsActive);

        if (updated == null)
        {
            return NotFound(new { message = "委托记录不存在" });
        }

        return Ok(MapToResponse(updated));
    }

    /// <summary>
    /// 删除委托记录
    /// </summary>
    /// <param name="id">委托ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "认证信息无效" });
        }

        var success = await _delegationService.DeleteAsync(id, userId.Value);
        if (!success)
        {
            return NotFound(new { message = "委托记录不存在或无权删除" });
        }

        return Ok(new { message = "删除成功" });
    }

    /// <summary>
    /// 获取当前用户有效的委托人列表
    /// </summary>
    /// <returns>有效的委托记录列表</returns>
    [HttpGet("active-grantors")]
    public async Task<IActionResult> GetActiveGrantors()
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "认证信息无效" });
        }

        var delegations = await _delegationService.GetActiveGrantorsAsync(userId.Value);
        return Ok(delegations.Select(d => new
        {
            id = d.Id,
            grantorId = d.GrantorId,
            grantorName = d.Grantor?.DisplayName,
            grantorRole = d.Grantor?.Role.ToString(),
            startDate = d.StartDate,
            endDate = d.EndDate,
            reason = d.Reason
        }));
    }

    /// <summary>
    /// 将委托实体映射为响应DTO
    /// </summary>
    /// <param name="delegation">委托实体</param>
    /// <returns>响应对象</returns>
    private static object MapToResponse(Delegation delegation)
    {
        return new
        {
            id = delegation.Id,
            grantorId = delegation.GrantorId,
            grantorName = delegation.Grantor?.DisplayName,
            grantorRole = delegation.Grantor?.Role.ToString(),
            trusteeId = delegation.TrusteeId,
            trusteeName = delegation.Trustee?.DisplayName,
            trusteeRole = delegation.Trustee?.Role.ToString(),
            startDate = delegation.StartDate,
            endDate = delegation.EndDate,
            reason = delegation.Reason,
            isActive = delegation.IsActive,
            createdAt = delegation.CreatedAt
        };
    }
}
