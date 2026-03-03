using Microsoft.EntityFrameworkCore;
using PurchaseApproval.Data;
using PurchaseApproval.Models;

namespace PurchaseApproval.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;

    public NotificationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Notification>> GetByUserIdAsync(Guid userId, bool? unreadOnly = null)
    {
        var query = _context.Notifications.Where(n => n.UserId == userId);

        if (unreadOnly == true)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query.OrderByDescending(n => n.CreatedAt).ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification == null)
        {
            return false;
        }

        notification.IsRead = true;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MarkAllAsReadAsync(Guid userId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task CreateAsync(Guid userId, string title, string content, Guid? requestId = null)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Content = content,
            RequestId = requestId
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
    }

    public async Task NotifyApprovalResultAsync(PurchaseRequest request, ApprovalAction action, string approverName)
    {
        var actionText = action switch
        {
            ApprovalAction.Approve => "已通过",
            ApprovalAction.Reject => "已拒绝",
            ApprovalAction.Return => "已退回",
            _ => "已处理"
        };

        var title = $"采购申请{actionText}";
        var content = $"您的采购申请 [{request.RequestNumber}] {request.ItemName} 已被 {approverName} {actionText}";

        await CreateAsync(request.ApplicantId, title, content, request.Id);
    }

    public async Task NotifyPendingApprovalAsync(PurchaseRequest request, UserRole targetRole)
    {
        var approvers = await _context.Users.Where(u => u.Role == targetRole).ToListAsync();

        var title = "新的待审批申请";
        var content = $"有一笔新的采购申请 [{request.RequestNumber}] {request.ItemName} 等待您审批";

        foreach (var approver in approvers)
        {
            await CreateAsync(approver.Id, title, content, request.Id);
        }
    }
}
