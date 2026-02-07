using Microsoft.EntityFrameworkCore;
using PurchaseApproval.Data;
using PurchaseApproval.DTOs;
using PurchaseApproval.Models;
using PurchaseApproval.Workflows;
using WorkflowCore.Interface;

namespace PurchaseApproval.Services;

public class PurchaseRequestService : IPurchaseRequestService
{
    private readonly AppDbContext _context;
    private readonly IWorkflowHost _workflowHost;

    public PurchaseRequestService(AppDbContext context, IWorkflowHost workflowHost)
    {
        _context = context;
        _workflowHost = workflowHost;
    }

    public async Task<List<PurchaseRequest>> GetAllAsync(Guid? userId = null, RequestStatus? status = null)
    {
        var query = _context.PurchaseRequests
            .Include(r => r.Applicant)
            .AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(r => r.ApplicantId == userId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        return await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
    }

    public async Task<PurchaseRequest?> GetByIdAsync(Guid id)
    {
        return await _context.PurchaseRequests
            .Include(r => r.Applicant)
            .Include(r => r.ApprovalRecords)
                .ThenInclude(ar => ar.Approver)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<PurchaseRequest> CreateAsync(CreateRequestDto dto, Guid applicantId)
    {
        var request = new PurchaseRequest
        {
            Id = Guid.NewGuid(),
            RequestNumber = await GenerateRequestNumberAsync(),
            ApplicantId = applicantId,
            ItemName = dto.ItemName,
            Quantity = dto.Quantity,
            UnitPrice = dto.UnitPrice,
            Reason = dto.Reason ?? string.Empty,
            Urgency = dto.Urgency,
            Status = RequestStatus.Draft
        };

        _context.PurchaseRequests.Add(request);
        await _context.SaveChangesAsync();

        return request;
    }

    public async Task<PurchaseRequest?> UpdateAsync(Guid id, UpdateRequestDto dto)
    {
        var request = await _context.PurchaseRequests.FindAsync(id);
        if (request == null) return null;

        // 只有草稿和已退回状态可以编辑
        if (request.Status != RequestStatus.Draft && request.Status != RequestStatus.Returned)
        {
            throw new InvalidOperationException("只有草稿或已退回的申请可以编辑");
        }

        request.ItemName = dto.ItemName;
        request.Quantity = dto.Quantity;
        request.UnitPrice = dto.UnitPrice;
        request.Reason = dto.Reason ?? string.Empty;
        request.Urgency = dto.Urgency;
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return request;
    }

    public async Task<bool> SubmitAsync(Guid id)
    {
        var request = await _context.PurchaseRequests.FindAsync(id);
        if (request == null) return false;

        // 只有草稿和已退回状态可以提交
        if (request.Status != RequestStatus.Draft && request.Status != RequestStatus.Returned)
        {
            throw new InvalidOperationException("只有草稿或已退回的申请可以提交");
        }

        // 计算总金额并确定审批级别
        var totalAmount = request.Quantity * request.UnitPrice;
        request.Status = RequestStatus.Pending;
        request.CurrentApprovalLevel = 1;
        request.UpdatedAt = DateTime.UtcNow;

        // 启动工作流
        var workflowData = new PurchaseWorkflowData
        {
            RequestId = request.Id,
            TotalAmount = totalAmount
        };
        
        var workflowId = await _workflowHost.StartWorkflow(
            PurchaseApprovalWorkflow.WorkflowId, 
            workflowData
        );
        
        request.WorkflowId = workflowId;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> CancelAsync(Guid id)
    {
        var request = await _context.PurchaseRequests.FindAsync(id);
        if (request == null) return false;

        // 只有待审批状态可以撤销
        if (request.Status != RequestStatus.Pending && 
            request.Status != RequestStatus.ManagerApproved &&
            request.Status != RequestStatus.FinanceApproved)
        {
            throw new InvalidOperationException("只有审批中的申请可以撤销");
        }

        request.Status = RequestStatus.Cancelled;
        request.UpdatedAt = DateTime.UtcNow;

        // 终止工作流
        if (!string.IsNullOrEmpty(request.WorkflowId))
        {
            await _workflowHost.TerminateWorkflow(request.WorkflowId);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<string> GenerateRequestNumberAsync()
    {
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var prefix = $"PR{today}";
        
        var lastRequest = await _context.PurchaseRequests
            .Where(r => r.RequestNumber.StartsWith(prefix))
            .OrderByDescending(r => r.RequestNumber)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastRequest != null)
        {
            var lastNumber = lastRequest.RequestNumber.Substring(prefix.Length);
            if (int.TryParse(lastNumber, out int num))
            {
                nextNumber = num + 1;
            }
        }

        return $"{prefix}{nextNumber:D4}";
    }

    public async Task UpdateStatusAsync(Guid id, RequestStatus status, int? approvalLevel = null)
    {
        var request = await _context.PurchaseRequests.FindAsync(id);
        if (request == null) return;

        request.Status = status;
        if (approvalLevel.HasValue)
        {
            request.CurrentApprovalLevel = approvalLevel.Value;
        }
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }
}
