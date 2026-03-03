using PurchaseApproval.Models;

namespace PurchaseApproval.Services;

public interface INotificationService
{
    Task<List<Notification>> GetByUserIdAsync(Guid userId, bool? unreadOnly = null);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId);
    Task<bool> MarkAllAsReadAsync(Guid userId);
    Task CreateAsync(Guid userId, string title, string content, Guid? requestId = null);
    Task NotifyApprovalResultAsync(PurchaseRequest request, ApprovalAction action, string approverName);
    Task NotifyPendingApprovalAsync(PurchaseRequest request, UserRole targetRole);
}
