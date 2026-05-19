using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PurchaseApproval.DTOs;
using PurchaseApproval.Extensions;
using PurchaseApproval.Services;

namespace PurchaseApproval.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApprovalsController : ControllerBase
{
    private readonly IApprovalService _approvalService;

    public ApprovalsController(IApprovalService approvalService)
    {
        _approvalService = approvalService;
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPending()
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "认证信息无效" });
        }

        var requests = await _approvalService.GetPendingApprovalsAsync(userId.Value);
        return Ok(requests.Select(r => new
        {
            id = r.Id,
            requestNumber = r.RequestNumber,
            applicantId = r.ApplicantId,
            applicantName = r.Applicant?.DisplayName,
            applicantDepartment = r.Applicant?.Department,
            itemName = r.ItemName,
            quantity = r.Quantity,
            unitPrice = r.UnitPrice,
            totalAmount = r.TotalAmount,
            reason = r.Reason,
            urgency = r.Urgency.ToString(),
            urgencyValue = (int)r.Urgency,
            status = r.Status.ToString(),
            statusValue = (int)r.Status,
            currentApprovalLevel = r.CurrentApprovalLevel,
            createdAt = r.CreatedAt
        }));
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApprovalDto dto)
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

        var result = await _approvalService.ApproveAsync(id, userId.Value, dto.Comment);
        if (!result)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "审批失败，请检查权限或申请状态" });
        }

        return Ok(new { message = "审批通过" });
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] ApprovalDto dto)
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

        var result = await _approvalService.RejectAsync(id, userId.Value, dto.Comment);
        if (!result)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "操作失败，请检查权限或申请状态" });
        }

        return Ok(new { message = "已拒绝" });
    }

    [HttpPost("{id}/return")]
    public async Task<IActionResult> Return(Guid id, [FromBody] ApprovalDto dto)
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

        var result = await _approvalService.ReturnAsync(id, userId.Value, dto.Comment);
        if (!result)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "操作失败，请检查权限或申请状态" });
        }

        return Ok(new { message = "已退回" });
    }

    [HttpGet("{requestId}/history")]
    public async Task<IActionResult> GetHistory(Guid requestId)
    {
        var userId = User.GetUserId();
        var userRole = User.GetUserRole();
        if (!userId.HasValue || !userRole.HasValue)
        {
            return Unauthorized(new { message = "认证信息无效" });
        }

        try
        {
            var records = await _approvalService.GetApprovalHistoryAsync(requestId, userId.Value, userRole.Value);
            return Ok(records.Select(r => new
            {
                id = r.Id,
                approverId = r.ApproverId,
                approverName = r.Approver?.DisplayName,
                operatorId = r.OperatorId,
                operatorName = r.Operator?.DisplayName,
                isDelegated = r.IsDelegated,
                action = r.Action.ToString(),
                actionValue = (int)r.Action,
                comment = r.Comment,
                approvalLevel = r.ApprovalLevel,
                createdAt = r.CreatedAt
            }));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }
}
