using Microsoft.AspNetCore.Mvc;
using PurchaseApproval.DTOs;
using PurchaseApproval.Services;

namespace PurchaseApproval.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApprovalsController : ControllerBase
{
    private readonly IApprovalService _approvalService;

    public ApprovalsController(IApprovalService approvalService)
    {
        _approvalService = approvalService;
    }

    /// <summary>
    /// 获取待审批列表
    /// </summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending([FromHeader(Name = "X-User-Id")] Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return BadRequest(new { message = "请先登录" });
        }

        var requests = await _approvalService.GetPendingApprovalsAsync(userId);
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
            status = r.Status.ToString(),
            currentApprovalLevel = r.CurrentApprovalLevel,
            createdAt = r.CreatedAt
        }));
    }

    /// <summary>
    /// 同意审批
    /// </summary>
    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApprovalDto dto, [FromHeader(Name = "X-User-Id")] Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return BadRequest(new { message = "请先登录" });
        }

        var result = await _approvalService.ApproveAsync(id, userId, dto.Comment);
        if (!result)
        {
            return BadRequest(new { message = "审批失败，请检查权限或申请状态" });
        }
        return Ok(new { message = "审批通过" });
    }

    /// <summary>
    /// 拒绝审批
    /// </summary>
    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] ApprovalDto dto, [FromHeader(Name = "X-User-Id")] Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return BadRequest(new { message = "请先登录" });
        }

        var result = await _approvalService.RejectAsync(id, userId, dto.Comment);
        if (!result)
        {
            return BadRequest(new { message = "操作失败，请检查权限或申请状态" });
        }
        return Ok(new { message = "已拒绝" });
    }

    /// <summary>
    /// 退回修改
    /// </summary>
    [HttpPost("{id}/return")]
    public async Task<IActionResult> Return(Guid id, [FromBody] ApprovalDto dto, [FromHeader(Name = "X-User-Id")] Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return BadRequest(new { message = "请先登录" });
        }

        var result = await _approvalService.ReturnAsync(id, userId, dto.Comment);
        if (!result)
        {
            return BadRequest(new { message = "操作失败，请检查权限或申请状态" });
        }
        return Ok(new { message = "已退回" });
    }

    /// <summary>
    /// 获取审批历史
    /// </summary>
    [HttpGet("{requestId}/history")]
    public async Task<IActionResult> GetHistory(Guid requestId)
    {
        var records = await _approvalService.GetApprovalHistoryAsync(requestId);
        return Ok(records.Select(r => new
        {
            id = r.Id,
            approverId = r.ApproverId,
            approverName = r.Approver?.DisplayName,
            action = r.Action.ToString(),
            actionValue = (int)r.Action,
            comment = r.Comment,
            approvalLevel = r.ApprovalLevel,
            createdAt = r.CreatedAt
        }));
    }
}
