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

        // 计算总金额
        var totalAmount = request.Quantity * request.UnitPrice;
        
        // 直接查询申请人角色，确保获取正确的角色信息
        var applicant = await _context.Users.FindAsync(request.ApplicantId);
        var applicantRole = applicant?.Role ?? UserRole.Employee;
        
        // 根据申请人角色和金额确定审批起始级别
        var (startStatus, startLevel) = DetermineApprovalStart(applicantRole, totalAmount);
        
        request.Status = startStatus;
        request.CurrentApprovalLevel = startLevel;
        request.UpdatedAt = DateTime.UtcNow;

        // 如果直接通过，无需启动工作流
        if (startStatus == RequestStatus.Approved)
        {
            await _context.SaveChangesAsync();
            return true;
        }

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

    /// <summary>
    /// 根据申请人角色和金额确定审批起始状态和级别
    /// 规则：
    /// - 员工：任何金额从 Level 1 开始（经理审批）
    /// - 经理：≤5000 直接通过；>5000 从 Level 2 开始（财务审批）
    /// - 财务：≤20000 直接通过；>20000 从 Level 3 开始（总经理审批）
    /// - 总经理：任何金额直接通过
    /// - 管理员：视同员工
    /// </summary>
    private (RequestStatus status, int level) DetermineApprovalStart(UserRole role, decimal totalAmount)
    {
        switch (role)
        {
            case UserRole.Director: // 总经理
                // 总经理提交的申请直接通过
                return (RequestStatus.Approved, 3);

            case UserRole.Finance: // 财务总监
                if (totalAmount <= 20000)
                {
                    // 财务提交≤20000直接通过
                    return (RequestStatus.Approved, 2);
                }
                // 财务提交>20000需要总经理审批
                return (RequestStatus.FinanceApproved, 3);

            case UserRole.Manager: // 部门经理
                if (totalAmount <= 5000)
                {
                    // 经理提交≤5000直接通过
                    return (RequestStatus.Approved, 1);
                }
                // 经理提交>5000需要财务审批
                return (RequestStatus.ManagerApproved, 2);

            case UserRole.Employee: // 普通员工
            case UserRole.Admin: // 管理员视同员工
            default:
                // 员工提交从经理开始审批
                return (RequestStatus.Pending, 1);
        }
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
