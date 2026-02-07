using Microsoft.EntityFrameworkCore;
using PurchaseApproval.Data;
using PurchaseApproval.Models;
using PurchaseApproval.Workflows;
using WorkflowCore.Interface;

namespace PurchaseApproval.Services;

public class ApprovalService : IApprovalService
{
    private readonly AppDbContext _context;
    private readonly IWorkflowHost _workflowHost;
    private readonly INotificationService _notificationService;

    public ApprovalService(
        AppDbContext context, 
        IWorkflowHost workflowHost,
        INotificationService notificationService)
    {
        _context = context;
        _workflowHost = workflowHost;
        _notificationService = notificationService;
    }

    public async Task<List<PurchaseRequest>> GetPendingApprovalsAsync(Guid approverId)
    {
        var approver = await _context.Users.FindAsync(approverId);
        if (approver == null) return new List<PurchaseRequest>();

        var query = _context.PurchaseRequests
            .Include(r => r.Applicant)
            .AsQueryable();

        // 根据审批人角色筛选待审批的申请
        switch (approver.Role)
        {
            case UserRole.Manager:
                // 部门经理：审批 Pending 状态 (Level 1)
                query = query.Where(r => 
                    r.Status == RequestStatus.Pending && 
                    r.CurrentApprovalLevel == 1);
                break;
                
            case UserRole.Finance:
                // 财务总监：审批 ManagerApproved 状态 (Level 2)
                query = query.Where(r => 
                    r.Status == RequestStatus.ManagerApproved && 
                    r.CurrentApprovalLevel == 2);
                break;
                
            case UserRole.Director:
                // 总经理：审批 FinanceApproved 状态 (Level 3)
                query = query.Where(r => 
                    r.Status == RequestStatus.FinanceApproved && 
                    r.CurrentApprovalLevel == 3);
                break;
                
            default:
                return new List<PurchaseRequest>();
        }

        return await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
    }

    public async Task<bool> ApproveAsync(Guid requestId, Guid approverId, string? comment)
    {
        var request = await _context.PurchaseRequests.FindAsync(requestId);
        var approver = await _context.Users.FindAsync(approverId);
        
        if (request == null || approver == null) return false;

        // 验证审批权限
        if (!CanApprove(request, approver)) return false;

        // 记录审批
        var record = new ApprovalRecord
        {
            Id = Guid.NewGuid(),
            RequestId = requestId,
            ApproverId = approverId,
            Action = ApprovalAction.Approve,
            Comment = comment ?? string.Empty,
            ApprovalLevel = request.CurrentApprovalLevel
        };
        _context.ApprovalRecords.Add(record);

        // 计算总金额确定下一步
        var totalAmount = request.Quantity * request.UnitPrice;
        var nextStatus = DetermineNextStatus(request.Status, totalAmount);
        var nextLevel = DetermineNextLevel(request.CurrentApprovalLevel, totalAmount);

        request.Status = nextStatus;
        request.CurrentApprovalLevel = nextLevel;
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // 发布工作流事件
        if (!string.IsNullOrEmpty(request.WorkflowId))
        {
            await _workflowHost.PublishEvent(
                PurchaseApprovalWorkflow.ApprovalEventName,
                request.WorkflowId,
                new ApprovalEventData { Action = ApprovalAction.Approve }
            );
        }

        // 发送通知
        await _notificationService.NotifyApprovalResultAsync(request, ApprovalAction.Approve, approver.DisplayName);

        return true;
    }

    public async Task<bool> RejectAsync(Guid requestId, Guid approverId, string? comment)
    {
        var request = await _context.PurchaseRequests.FindAsync(requestId);
        var approver = await _context.Users.FindAsync(approverId);
        
        if (request == null || approver == null) return false;
        if (!CanApprove(request, approver)) return false;

        var record = new ApprovalRecord
        {
            Id = Guid.NewGuid(),
            RequestId = requestId,
            ApproverId = approverId,
            Action = ApprovalAction.Reject,
            Comment = comment ?? string.Empty,
            ApprovalLevel = request.CurrentApprovalLevel
        };
        _context.ApprovalRecords.Add(record);

        request.Status = RequestStatus.Rejected;
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        if (!string.IsNullOrEmpty(request.WorkflowId))
        {
            await _workflowHost.PublishEvent(
                PurchaseApprovalWorkflow.ApprovalEventName,
                request.WorkflowId,
                new ApprovalEventData { Action = ApprovalAction.Reject }
            );
        }

        await _notificationService.NotifyApprovalResultAsync(request, ApprovalAction.Reject, approver.DisplayName);

        return true;
    }

    public async Task<bool> ReturnAsync(Guid requestId, Guid approverId, string? comment)
    {
        var request = await _context.PurchaseRequests.FindAsync(requestId);
        var approver = await _context.Users.FindAsync(approverId);
        
        if (request == null || approver == null) return false;
        if (!CanApprove(request, approver)) return false;

        var record = new ApprovalRecord
        {
            Id = Guid.NewGuid(),
            RequestId = requestId,
            ApproverId = approverId,
            Action = ApprovalAction.Return,
            Comment = comment ?? string.Empty,
            ApprovalLevel = request.CurrentApprovalLevel
        };
        _context.ApprovalRecords.Add(record);

        request.Status = RequestStatus.Returned;
        request.CurrentApprovalLevel = 0;
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        if (!string.IsNullOrEmpty(request.WorkflowId))
        {
            await _workflowHost.PublishEvent(
                PurchaseApprovalWorkflow.ApprovalEventName,
                request.WorkflowId,
                new ApprovalEventData { Action = ApprovalAction.Return }
            );
        }

        await _notificationService.NotifyApprovalResultAsync(request, ApprovalAction.Return, approver.DisplayName);

        return true;
    }

    public async Task<List<ApprovalRecord>> GetApprovalHistoryAsync(Guid requestId)
    {
        return await _context.ApprovalRecords
            .Include(ar => ar.Approver)
            .Where(ar => ar.RequestId == requestId)
            .OrderBy(ar => ar.CreatedAt)
            .ToListAsync();
    }

    private bool CanApprove(PurchaseRequest request, User approver)
    {
        return (approver.Role == UserRole.Manager && request.Status == RequestStatus.Pending && request.CurrentApprovalLevel == 1) ||
               (approver.Role == UserRole.Finance && request.Status == RequestStatus.ManagerApproved && request.CurrentApprovalLevel == 2) ||
               (approver.Role == UserRole.Director && request.Status == RequestStatus.FinanceApproved && request.CurrentApprovalLevel == 3);
    }

    private RequestStatus DetermineNextStatus(RequestStatus currentStatus, decimal totalAmount)
    {
        switch (currentStatus)
        {
            case RequestStatus.Pending:
                // 经理审批后
                if (totalAmount <= 5000) return RequestStatus.Approved;
                return RequestStatus.ManagerApproved;
                
            case RequestStatus.ManagerApproved:
                // 财务审批后
                if (totalAmount <= 20000) return RequestStatus.Approved;
                return RequestStatus.FinanceApproved;
                
            case RequestStatus.FinanceApproved:
                // 总经理审批后
                return RequestStatus.Approved;
                
            default:
                return currentStatus;
        }
    }

    private int DetermineNextLevel(int currentLevel, decimal totalAmount)
    {
        switch (currentLevel)
        {
            case 1:
                if (totalAmount <= 5000) return 1;
                return 2;
            case 2:
                if (totalAmount <= 20000) return 2;
                return 3;
            case 3:
                return 3;
            default:
                return 1;
        }
    }
}
