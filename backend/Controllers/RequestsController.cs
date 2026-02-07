using Microsoft.AspNetCore.Mvc;
using PurchaseApproval.DTOs;
using PurchaseApproval.Models;
using PurchaseApproval.Services;

namespace PurchaseApproval.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RequestsController : ControllerBase
{
    private readonly IPurchaseRequestService _requestService;

    public RequestsController(IPurchaseRequestService requestService)
    {
        _requestService = requestService;
    }

    /// <summary>
    /// 获取采购申请列表
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? userId, [FromQuery] RequestStatus? status)
    {
        var requests = await _requestService.GetAllAsync(userId, status);
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

    /// <summary>
    /// 获取采购申请详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var request = await _requestService.GetByIdAsync(id);
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

    /// <summary>
    /// 创建采购申请
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRequestDto dto, [FromHeader(Name = "X-User-Id")] Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return BadRequest(new { message = "请先登录" });
        }

        try
        {
            var request = await _requestService.CreateAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { id = request.Id }, new
            {
                id = request.Id,
                requestNumber = request.RequestNumber,
                message = "申请创建成功"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// 更新采购申请
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRequestDto dto)
    {
        try
        {
            var request = await _requestService.UpdateAsync(id, dto);
            if (request == null)
            {
                return NotFound(new { message = "申请不存在" });
            }
            return Ok(new { message = "更新成功" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// 提交采购申请
    /// </summary>
    [HttpPost("{id}/submit")]
    public async Task<IActionResult> Submit(Guid id)
    {
        try
        {
            var result = await _requestService.SubmitAsync(id);
            if (!result)
            {
                return NotFound(new { message = "申请不存在" });
            }
            return Ok(new { message = "提交成功，已进入审批流程" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// 撤销采购申请
    /// </summary>
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        try
        {
            var result = await _requestService.CancelAsync(id);
            if (!result)
            {
                return NotFound(new { message = "申请不存在" });
            }
            return Ok(new { message = "撤销成功" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
