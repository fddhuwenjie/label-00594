using PurchaseApproval.Models;

namespace PurchaseApproval.Services;

public interface IApprovalService
{
    Task<List<PurchaseRequest>> GetPendingApprovalsAsync(Guid approverId);
    Task<bool> ApproveAsync(Guid requestId, Guid approverId, string? comment);
    Task<bool> RejectAsync(Guid requestId, Guid approverId, string? comment);
    Task<bool> ReturnAsync(Guid requestId, Guid approverId, string? comment);
    Task<List<ApprovalRecord>> GetApprovalHistoryAsync(Guid requestId, Guid operatorId, UserRole operatorRole);
}
