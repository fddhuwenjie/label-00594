using Microsoft.EntityFrameworkCore;
using PurchaseApproval.Data;
using PurchaseApproval.DTOs;
using PurchaseApproval.Models;
using PurchaseApproval.Utils;
using PurchaseApproval.Workflows;
using WorkflowCore.Interface;

namespace PurchaseApproval.Services;

public class PurchaseRequestService : IPurchaseRequestService
{
    private readonly AppDbContext _context;
    private readonly IWorkflowHost _workflowHost;
    private readonly IAuditLogService _auditLogService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<PurchaseRequestService> _logger;

    public PurchaseRequestService(
        AppDbContext context,
        IWorkflowHost workflowHost,
        IAuditLogService auditLogService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PurchaseRequestService> logger)
    {
        _context = context;
        _workflowHost = workflowHost;
        _auditLogService = auditLogService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<List<PurchaseRequest>> GetAllAsync(Guid userId, UserRole role, RequestStatus? status = null)
    {
        var query = _context.PurchaseRequests
            .Include(r => r.Applicant)
            .AsQueryable();

        if (role != UserRole.Admin)
        {
            query = query.Where(r => r.ApplicantId == userId);
        }

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        return await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
    }

    public async Task<PurchaseRequest?> GetByIdAsync(Guid id, Guid userId, UserRole role)
    {
        var request = await _context.PurchaseRequests
            .Include(r => r.Applicant)
            .Include(r => r.ApprovalRecords)
                .ThenInclude(ar => ar.Approver)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null)
        {
            return null;
        }

        if (!CanAccessRequest(request, userId, role))
        {
            throw new UnauthorizedAccessException("无权访问该采购申请");
        }

        return request;
    }

    public async Task<PurchaseRequest> CreateAsync(CreateRequestDto dto, Guid applicantId)
    {
        ValidateRequestInput(dto.ItemName, dto.Quantity, dto.UnitPrice, dto.Reason, dto.Urgency);

        var now = DateTimeHelper.GetBeijingTime();
        var request = new PurchaseRequest
        {
            Id = Guid.NewGuid(),
            RequestNumber = await GenerateRequestNumberAsync(),
            ApplicantId = applicantId,
            ItemName = dto.ItemName.Trim(),
            Quantity = dto.Quantity,
            UnitPrice = decimal.Round(dto.UnitPrice, 2),
            Reason = dto.Reason?.Trim() ?? string.Empty,
            Urgency = dto.Urgency,
            Status = RequestStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.PurchaseRequests.Add(request);
        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            action: "PurchaseRequest.Create",
            resourceType: "PurchaseRequest",
            resourceId: request.Id.ToString(),
            result: "Success",
            details: $"requestNumber={request.RequestNumber};amount={request.TotalAmount}",
            actorId: applicantId);

        _logger.LogInformation(
            "采购申请创建成功: requestId={RequestId}, applicantId={ApplicantId}, requestNumber={RequestNumber}",
            request.Id,
            applicantId,
            request.RequestNumber);

        return request;
    }

    public async Task<PurchaseRequest?> UpdateAsync(Guid id, UpdateRequestDto dto, Guid currentUserId, UserRole role)
    {
        var request = await _context.PurchaseRequests.FindAsync(id);
        if (request == null)
        {
            return null;
        }

        EnsureOwnerOrAdmin(request, currentUserId, role, "PurchaseRequest.Update");
        ValidateRequestInput(dto.ItemName, dto.Quantity, dto.UnitPrice, dto.Reason, dto.Urgency);

        if (request.Status != RequestStatus.Draft && request.Status != RequestStatus.Returned)
        {
            throw new InvalidOperationException("只有草稿或已退回的申请可以编辑");
        }

        request.ItemName = dto.ItemName.Trim();
        request.Quantity = dto.Quantity;
        request.UnitPrice = decimal.Round(dto.UnitPrice, 2);
        request.Reason = dto.Reason?.Trim() ?? string.Empty;
        request.Urgency = dto.Urgency;
        request.UpdatedAt = DateTimeHelper.GetBeijingTime();

        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            action: "PurchaseRequest.Update",
            resourceType: "PurchaseRequest",
            resourceId: request.Id.ToString(),
            result: "Success",
            details: $"status={request.Status};amount={request.TotalAmount}",
            actorId: currentUserId,
            actorRole: role.ToString());

        _logger.LogInformation(
            "采购申请更新成功: requestId={RequestId}, actorId={ActorId}, role={Role}",
            request.Id,
            currentUserId,
            role);

        return request;
    }

    public async Task<bool> SubmitAsync(Guid id, Guid currentUserId, UserRole role)
    {
        var request = await _context.PurchaseRequests.FindAsync(id);
        if (request == null)
        {
            return false;
        }

        EnsureOwnerOrAdmin(request, currentUserId, role, "PurchaseRequest.Submit");

        if (request.Status != RequestStatus.Draft && request.Status != RequestStatus.Returned)
        {
            throw new InvalidOperationException("只有草稿或已退回的申请可以提交");
        }

        ValidateRequestInput(request.ItemName, request.Quantity, request.UnitPrice, request.Reason, request.Urgency);

        var totalAmount = request.Quantity * request.UnitPrice;

        var applicant = await _context.Users.FindAsync(request.ApplicantId);
        var applicantRole = applicant?.Role ?? UserRole.Employee;

        var (startStatus, startLevel) = DetermineApprovalStart(applicantRole, totalAmount);

        request.Status = startStatus;
        request.CurrentApprovalLevel = startLevel;
        request.UpdatedAt = DateTimeHelper.GetBeijingTime();

        if (startStatus == RequestStatus.Approved)
        {
            await _context.SaveChangesAsync();
            await _auditLogService.LogAsync(
                action: "PurchaseRequest.Submit",
                resourceType: "PurchaseRequest",
                resourceId: request.Id.ToString(),
                result: "Success",
                details: "directlyApproved=true",
                actorId: currentUserId,
                actorRole: role.ToString());
            return true;
        }

        var workflowData = new PurchaseWorkflowData
        {
            RequestId = request.Id,
            TotalAmount = totalAmount,
            CorrelationId = _httpContextAccessor.HttpContext?.TraceIdentifier
        };

        var workflowId = await _workflowHost.StartWorkflow(
            PurchaseApprovalWorkflow.WorkflowId,
            workflowData
        );

        request.WorkflowId = workflowId;
        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            action: "PurchaseRequest.Submit",
            resourceType: "PurchaseRequest",
            resourceId: request.Id.ToString(),
            result: "Success",
            details: $"workflowId={workflowId};startLevel={startLevel};status={startStatus}",
            actorId: currentUserId,
            actorRole: role.ToString());

        _logger.LogInformation(
            "采购申请提交成功: requestId={RequestId}, actorId={ActorId}, workflowId={WorkflowId}, status={Status}",
            request.Id,
            currentUserId,
            workflowId,
            startStatus);

        return true;
    }

    public async Task<bool> CancelAsync(Guid id, Guid currentUserId, UserRole role)
    {
        var request = await _context.PurchaseRequests.FindAsync(id);
        if (request == null)
        {
            return false;
        }

        EnsureOwnerOrAdmin(request, currentUserId, role, "PurchaseRequest.Cancel");

        if (request.Status != RequestStatus.Pending &&
            request.Status != RequestStatus.ManagerApproved &&
            request.Status != RequestStatus.FinanceApproved)
        {
            throw new InvalidOperationException("只有审批中的申请可以撤销");
        }

        request.Status = RequestStatus.Cancelled;
        request.UpdatedAt = DateTimeHelper.GetBeijingTime();

        if (!string.IsNullOrEmpty(request.WorkflowId))
        {
            await _workflowHost.TerminateWorkflow(request.WorkflowId);
        }

        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            action: "PurchaseRequest.Cancel",
            resourceType: "PurchaseRequest",
            resourceId: request.Id.ToString(),
            result: "Success",
            details: $"workflowId={request.WorkflowId}",
            actorId: currentUserId,
            actorRole: role.ToString());

        _logger.LogInformation(
            "采购申请撤销成功: requestId={RequestId}, actorId={ActorId}, role={Role}",
            request.Id,
            currentUserId,
            role);

        return true;
    }

    private static (RequestStatus status, int level) DetermineApprovalStart(UserRole role, decimal totalAmount)
    {
        return role switch
        {
            UserRole.Director => (RequestStatus.Approved, 3),
            UserRole.Finance when totalAmount <= 20000 => (RequestStatus.Approved, 2),
            UserRole.Finance => (RequestStatus.FinanceApproved, 3),
            UserRole.Manager when totalAmount <= 5000 => (RequestStatus.Approved, 1),
            UserRole.Manager => (RequestStatus.ManagerApproved, 2),
            _ => (RequestStatus.Pending, 1)
        };
    }

    public async Task<string> GenerateRequestNumberAsync()
    {
        var today = DateTimeHelper.GetBeijingTime().ToString("yyyyMMdd");
        var prefix = $"PR{today}";

        var lastRequest = await _context.PurchaseRequests
            .Where(r => r.RequestNumber.StartsWith(prefix))
            .OrderByDescending(r => r.RequestNumber)
            .FirstOrDefaultAsync();

        var nextNumber = 1;
        if (lastRequest != null)
        {
            var lastNumber = lastRequest.RequestNumber.Substring(prefix.Length);
            if (int.TryParse(lastNumber, out var num))
            {
                nextNumber = num + 1;
            }
        }

        return $"{prefix}{nextNumber:D4}";
    }

    public async Task UpdateStatusAsync(Guid id, RequestStatus status, int? approvalLevel = null)
    {
        var request = await _context.PurchaseRequests.FindAsync(id);
        if (request == null)
        {
            return;
        }

        request.Status = status;
        if (approvalLevel.HasValue)
        {
            request.CurrentApprovalLevel = approvalLevel.Value;
        }

        request.UpdatedAt = DateTimeHelper.GetBeijingTime();

        await _context.SaveChangesAsync();
    }

    private static bool CanAccessRequest(PurchaseRequest request, Guid userId, UserRole role)
    {
        if (role == UserRole.Admin)
        {
            return true;
        }

        var canApprove = role switch
        {
            UserRole.Manager => request.Status == RequestStatus.Pending && request.CurrentApprovalLevel == 1,
            UserRole.Finance => request.Status == RequestStatus.ManagerApproved && request.CurrentApprovalLevel == 2,
            UserRole.Director => request.Status == RequestStatus.FinanceApproved && request.CurrentApprovalLevel == 3,
            _ => false
        };

        return request.ApplicantId == userId || canApprove;
    }

    private void EnsureOwnerOrAdmin(PurchaseRequest request, Guid currentUserId, UserRole role, string action)
    {
        if (request.ApplicantId == currentUserId || role == UserRole.Admin)
        {
            return;
        }

        _logger.LogWarning(
            "拒绝敏感操作: action={Action}, requestId={RequestId}, actorId={ActorId}, role={Role}",
            action,
            request.Id,
            currentUserId,
            role);

        throw new UnauthorizedAccessException("仅申请人本人可执行该操作");
    }

    private static void ValidateRequestInput(string itemName, int quantity, decimal unitPrice, string? reason, Urgency urgency)
    {
        if (string.IsNullOrWhiteSpace(itemName))
        {
            throw new InvalidOperationException("物品名称不能为空白");
        }

        if (itemName.Trim().Length > 200)
        {
            throw new InvalidOperationException("物品名称不能超过 200 字");
        }

        if (quantity < 1 || quantity > 100000)
        {
            throw new InvalidOperationException("采购数量必须在 1 到 100000 之间");
        }

        if (unitPrice <= 0 || unitPrice > 999999999999.99m)
        {
            throw new InvalidOperationException("单价必须在 0.01 到 999999999999.99 之间");
        }

        if (decimal.Round(unitPrice, 2) != unitPrice)
        {
            throw new InvalidOperationException("单价最多保留两位小数");
        }

        if (!Enum.IsDefined(typeof(Urgency), urgency))
        {
            throw new InvalidOperationException("紧急程度无效");
        }

        if (!string.IsNullOrEmpty(reason) && reason.Length > 1000)
        {
            throw new InvalidOperationException("采购原因不能超过 1000 字");
        }
    }
}
