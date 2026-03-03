using Microsoft.EntityFrameworkCore;
using PurchaseApproval.Data;
using PurchaseApproval.Models;
using PurchaseApproval.Utils;
using PurchaseApproval.Workflows;
using WorkflowCore.Interface;

namespace PurchaseApproval.Services;

public class ApprovalService : IApprovalService
{
    private readonly AppDbContext _context;
    private readonly IWorkflowHost _workflowHost;
    private readonly INotificationService _notificationService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<ApprovalService> _logger;

    public ApprovalService(
        AppDbContext context,
        IWorkflowHost workflowHost,
        INotificationService notificationService,
        IAuditLogService auditLogService,
        ILogger<ApprovalService> logger)
    {
        _context = context;
        _workflowHost = workflowHost;
        _notificationService = notificationService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<List<PurchaseRequest>> GetPendingApprovalsAsync(Guid approverId)
    {
        var approver = await _context.Users.FindAsync(approverId);
        if (approver == null)
        {
            return new List<PurchaseRequest>();
        }

        var query = _context.PurchaseRequests
            .Include(r => r.Applicant)
            .AsQueryable();

        switch (approver.Role)
        {
            case UserRole.Manager:
                query = query.Where(r =>
                    r.Status == RequestStatus.Pending &&
                    r.CurrentApprovalLevel == 1);
                break;

            case UserRole.Finance:
                query = query.Where(r =>
                    r.Status == RequestStatus.ManagerApproved &&
                    r.CurrentApprovalLevel == 2);
                break;

            case UserRole.Director:
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

        if (request == null || approver == null)
        {
            return false;
        }

        if (!CanApprove(request, approver))
        {
            await _auditLogService.LogAsync(
                action: "Approval.Approve",
                resourceType: "PurchaseRequest",
                resourceId: requestId.ToString(),
                result: "Forbidden",
                details: "审批人权限不足或申请状态不匹配",
                actorId: approverId,
                actorUsername: approver.Username,
                actorRole: approver.Role.ToString());
            return false;
        }

        var record = new ApprovalRecord
        {
            Id = Guid.NewGuid(),
            RequestId = requestId,
            ApproverId = approverId,
            Action = ApprovalAction.Approve,
            Comment = comment?.Trim() ?? string.Empty,
            ApprovalLevel = request.CurrentApprovalLevel,
            CreatedAt = DateTimeHelper.GetBeijingTime()
        };
        _context.ApprovalRecords.Add(record);

        var totalAmount = request.Quantity * request.UnitPrice;
        var nextStatus = DetermineNextStatus(request.Status, totalAmount);
        var nextLevel = DetermineNextLevel(request.CurrentApprovalLevel, totalAmount);

        request.Status = nextStatus;
        request.CurrentApprovalLevel = nextLevel;
        request.UpdatedAt = DateTimeHelper.GetBeijingTime();

        await _context.SaveChangesAsync();

        if (!string.IsNullOrEmpty(request.WorkflowId))
        {
            await _workflowHost.PublishEvent(
                PurchaseApprovalWorkflow.ApprovalEventName,
                request.WorkflowId,
                new ApprovalEventData { Action = ApprovalAction.Approve }
            );
        }

        await _notificationService.NotifyApprovalResultAsync(request, ApprovalAction.Approve, approver.DisplayName);

        await _auditLogService.LogAsync(
            action: "Approval.Approve",
            resourceType: "PurchaseRequest",
            resourceId: requestId.ToString(),
            result: "Success",
            details: $"nextStatus={nextStatus};nextLevel={nextLevel}",
            actorId: approverId,
            actorUsername: approver.Username,
            actorRole: approver.Role.ToString());

        _logger.LogInformation(
            "审批通过: requestId={RequestId}, approverId={ApproverId}, nextStatus={NextStatus}, nextLevel={NextLevel}",
            requestId,
            approverId,
            nextStatus,
            nextLevel);

        return true;
    }

    public async Task<bool> RejectAsync(Guid requestId, Guid approverId, string? comment)
    {
        var request = await _context.PurchaseRequests.FindAsync(requestId);
        var approver = await _context.Users.FindAsync(approverId);

        if (request == null || approver == null)
        {
            return false;
        }

        if (!CanApprove(request, approver))
        {
            await _auditLogService.LogAsync(
                action: "Approval.Reject",
                resourceType: "PurchaseRequest",
                resourceId: requestId.ToString(),
                result: "Forbidden",
                details: "审批人权限不足或申请状态不匹配",
                actorId: approverId,
                actorUsername: approver.Username,
                actorRole: approver.Role.ToString());
            return false;
        }

        var record = new ApprovalRecord
        {
            Id = Guid.NewGuid(),
            RequestId = requestId,
            ApproverId = approverId,
            Action = ApprovalAction.Reject,
            Comment = comment?.Trim() ?? string.Empty,
            ApprovalLevel = request.CurrentApprovalLevel,
            CreatedAt = DateTimeHelper.GetBeijingTime()
        };
        _context.ApprovalRecords.Add(record);

        request.Status = RequestStatus.Rejected;
        request.UpdatedAt = DateTimeHelper.GetBeijingTime();

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

        await _auditLogService.LogAsync(
            action: "Approval.Reject",
            resourceType: "PurchaseRequest",
            resourceId: requestId.ToString(),
            result: "Success",
            details: $"status={request.Status}",
            actorId: approverId,
            actorUsername: approver.Username,
            actorRole: approver.Role.ToString());

        _logger.LogInformation(
            "审批拒绝: requestId={RequestId}, approverId={ApproverId}",
            requestId,
            approverId);

        return true;
    }

    public async Task<bool> ReturnAsync(Guid requestId, Guid approverId, string? comment)
    {
        var request = await _context.PurchaseRequests.FindAsync(requestId);
        var approver = await _context.Users.FindAsync(approverId);

        if (request == null || approver == null)
        {
            return false;
        }

        if (!CanApprove(request, approver))
        {
            await _auditLogService.LogAsync(
                action: "Approval.Return",
                resourceType: "PurchaseRequest",
                resourceId: requestId.ToString(),
                result: "Forbidden",
                details: "审批人权限不足或申请状态不匹配",
                actorId: approverId,
                actorUsername: approver.Username,
                actorRole: approver.Role.ToString());
            return false;
        }

        var record = new ApprovalRecord
        {
            Id = Guid.NewGuid(),
            RequestId = requestId,
            ApproverId = approverId,
            Action = ApprovalAction.Return,
            Comment = comment?.Trim() ?? string.Empty,
            ApprovalLevel = request.CurrentApprovalLevel,
            CreatedAt = DateTimeHelper.GetBeijingTime()
        };
        _context.ApprovalRecords.Add(record);

        request.Status = RequestStatus.Returned;
        request.CurrentApprovalLevel = 0;
        request.UpdatedAt = DateTimeHelper.GetBeijingTime();

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

        await _auditLogService.LogAsync(
            action: "Approval.Return",
            resourceType: "PurchaseRequest",
            resourceId: requestId.ToString(),
            result: "Success",
            details: $"status={request.Status}",
            actorId: approverId,
            actorUsername: approver.Username,
            actorRole: approver.Role.ToString());

        _logger.LogInformation(
            "审批退回: requestId={RequestId}, approverId={ApproverId}",
            requestId,
            approverId);

        return true;
    }

    public async Task<List<ApprovalRecord>> GetApprovalHistoryAsync(Guid requestId, Guid operatorId, UserRole operatorRole)
    {
        var request = await _context.PurchaseRequests.FindAsync(requestId);
        if (request == null)
        {
            return new List<ApprovalRecord>();
        }

        var canAccess = operatorRole == UserRole.Admin || request.ApplicantId == operatorId;
        if (!canAccess)
        {
            var approver = await _context.Users.FindAsync(operatorId);
            canAccess = approver != null && CanApprove(request, approver);
        }

        if (!canAccess)
        {
            await _auditLogService.LogAsync(
                action: "Approval.History",
                resourceType: "PurchaseRequest",
                resourceId: requestId.ToString(),
                result: "Forbidden",
                details: "无权查看审批历史",
                actorId: operatorId,
                actorRole: operatorRole.ToString());
            throw new UnauthorizedAccessException("无权查看该审批历史");
        }

        return await _context.ApprovalRecords
            .Include(ar => ar.Approver)
            .Where(ar => ar.RequestId == requestId)
            .OrderBy(ar => ar.CreatedAt)
            .ToListAsync();
    }

    private static bool CanApprove(PurchaseRequest request, User approver)
    {
        return (approver.Role == UserRole.Manager && request.Status == RequestStatus.Pending && request.CurrentApprovalLevel == 1) ||
               (approver.Role == UserRole.Finance && request.Status == RequestStatus.ManagerApproved && request.CurrentApprovalLevel == 2) ||
               (approver.Role == UserRole.Director && request.Status == RequestStatus.FinanceApproved && request.CurrentApprovalLevel == 3);
    }

    private static RequestStatus DetermineNextStatus(RequestStatus currentStatus, decimal totalAmount)
    {
        return currentStatus switch
        {
            RequestStatus.Pending when totalAmount <= 5000 => RequestStatus.Approved,
            RequestStatus.Pending => RequestStatus.ManagerApproved,
            RequestStatus.ManagerApproved when totalAmount <= 20000 => RequestStatus.Approved,
            RequestStatus.ManagerApproved => RequestStatus.FinanceApproved,
            RequestStatus.FinanceApproved => RequestStatus.Approved,
            _ => currentStatus
        };
    }

    private static int DetermineNextLevel(int currentLevel, decimal totalAmount)
    {
        return currentLevel switch
        {
            1 when totalAmount <= 5000 => 1,
            1 => 2,
            2 when totalAmount <= 20000 => 2,
            2 => 3,
            3 => 3,
            _ => 1
        };
    }
}
