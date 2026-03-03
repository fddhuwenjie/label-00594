using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PurchaseApproval.DTOs;
using PurchaseApproval.Extensions;
using PurchaseApproval.Models;
using PurchaseApproval.Services;

namespace PurchaseApproval.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RequestsController : ControllerBase
{
    private readonly IPurchaseRequestService _requestService;
    private readonly IAuditLogService _auditLogService;

    public RequestsController(IPurchaseRequestService requestService, IAuditLogService auditLogService)
    {
        _requestService = requestService;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] RequestStatus? status)
    {
        var currentUserId = User.GetUserId();
        var currentUserRole = User.GetUserRole();

        if (!currentUserId.HasValue || !currentUserRole.HasValue)
        {
            return Unauthorized(new { message = "认证信息无效" });
        }

        var requests = await _requestService.GetAllAsync(currentUserId.Value, currentUserRole.Value, status);
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
            createdAt = r.CreatedAt,
            updatedAt = r.UpdatedAt
        }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var currentUserId = User.GetUserId();
        var currentUserRole = User.GetUserRole();

        if (!currentUserId.HasValue || !currentUserRole.HasValue)
        {
            return Unauthorized(new { message = "认证信息无效" });
        }

        try
        {
            var request = await _requestService.GetByIdAsync(id, currentUserId.Value, currentUserRole.Value);
            if (request == null)
            {
                return NotFound(new { message = "申请不存在" });
            }

            return Ok(new
            {
                id = request.Id,
                requestNumber = request.RequestNumber,
                applicantId = request.ApplicantId,
                applicantName = request.Applicant?.DisplayName,
                applicantDepartment = request.Applicant?.Department,
                itemName = request.ItemName,
                quantity = request.Quantity,
                unitPrice = request.UnitPrice,
                totalAmount = request.TotalAmount,
                reason = request.Reason,
                urgency = request.Urgency.ToString(),
                urgencyValue = (int)request.Urgency,
                status = request.Status.ToString(),
                statusValue = (int)request.Status,
                currentApprovalLevel = request.CurrentApprovalLevel,
                workflowId = request.WorkflowId,
                createdAt = request.CreatedAt,
                updatedAt = request.UpdatedAt,
                approvalRecords = request.ApprovalRecords.OrderBy(ar => ar.CreatedAt).Select(ar => new
                {
                    id = ar.Id,
                    approverId = ar.ApproverId,
                    approverName = ar.Approver?.DisplayName,
                    action = ar.Action.ToString(),
                    actionValue = (int)ar.Action,
                    comment = ar.Comment,
                    approvalLevel = ar.ApprovalLevel,
                    createdAt = ar.CreatedAt
                })
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            await _auditLogService.LogAsync(
                action: "PurchaseRequest.Read",
                resourceType: "PurchaseRequest",
                resourceId: id.ToString(),
                result: "Forbidden",
                details: ex.Message,
                actorId: currentUserId,
                actorRole: currentUserRole.ToString());
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var currentUserId = User.GetUserId();
        if (!currentUserId.HasValue)
        {
            return Unauthorized(new { message = "认证信息无效" });
        }

        try
        {
            var request = await _requestService.CreateAsync(dto, currentUserId.Value);
            return CreatedAtAction(nameof(GetById), new { id = request.Id }, new
            {
                id = request.Id,
                requestNumber = request.RequestNumber,
                message = "申请创建成功"
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var currentUserId = User.GetUserId();
        var currentUserRole = User.GetUserRole();
        if (!currentUserId.HasValue || !currentUserRole.HasValue)
        {
            return Unauthorized(new { message = "认证信息无效" });
        }

        try
        {
            var request = await _requestService.UpdateAsync(id, dto, currentUserId.Value, currentUserRole.Value);
            if (request == null)
            {
                return NotFound(new { message = "申请不存在" });
            }

            return Ok(new { message = "更新成功" });
        }
        catch (UnauthorizedAccessException ex)
        {
            await _auditLogService.LogAsync(
                action: "PurchaseRequest.Update",
                resourceType: "PurchaseRequest",
                resourceId: id.ToString(),
                result: "Forbidden",
                details: ex.Message,
                actorId: currentUserId,
                actorRole: currentUserRole.ToString());
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/submit")]
    public async Task<IActionResult> Submit(Guid id)
    {
        var currentUserId = User.GetUserId();
        var currentUserRole = User.GetUserRole();
        if (!currentUserId.HasValue || !currentUserRole.HasValue)
        {
            return Unauthorized(new { message = "认证信息无效" });
        }

        try
        {
            var result = await _requestService.SubmitAsync(id, currentUserId.Value, currentUserRole.Value);
            if (!result)
            {
                return NotFound(new { message = "申请不存在" });
            }

            return Ok(new { message = "提交成功，已进入审批流程" });
        }
        catch (UnauthorizedAccessException ex)
        {
            await _auditLogService.LogAsync(
                action: "PurchaseRequest.Submit",
                resourceType: "PurchaseRequest",
                resourceId: id.ToString(),
                result: "Forbidden",
                details: ex.Message,
                actorId: currentUserId,
                actorRole: currentUserRole.ToString());
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var currentUserId = User.GetUserId();
        var currentUserRole = User.GetUserRole();
        if (!currentUserId.HasValue || !currentUserRole.HasValue)
        {
            return Unauthorized(new { message = "认证信息无效" });
        }

        try
        {
            var result = await _requestService.CancelAsync(id, currentUserId.Value, currentUserRole.Value);
            if (!result)
            {
                return NotFound(new { message = "申请不存在" });
            }

            return Ok(new { message = "撤销成功" });
        }
        catch (UnauthorizedAccessException ex)
        {
            await _auditLogService.LogAsync(
                action: "PurchaseRequest.Cancel",
                resourceType: "PurchaseRequest",
                resourceId: id.ToString(),
                result: "Forbidden",
                details: ex.Message,
                actorId: currentUserId,
                actorRole: currentUserRole.ToString());
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
