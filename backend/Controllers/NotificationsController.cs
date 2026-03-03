using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PurchaseApproval.Extensions;
using PurchaseApproval.Services;

namespace PurchaseApproval.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool? unreadOnly)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "认证信息无效" });
        }

        var notifications = await _notificationService.GetByUserIdAsync(userId.Value, unreadOnly);
        return Ok(notifications.Select(n => new
        {
            id = n.Id,
            title = n.Title,
            content = n.Content,
            requestId = n.RequestId,
            isRead = n.IsRead,
            createdAt = n.CreatedAt
        }));
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "认证信息无效" });
        }

        var count = await _notificationService.GetUnreadCountAsync(userId.Value);
        return Ok(new { count });
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "认证信息无效" });
        }

        var result = await _notificationService.MarkAsReadAsync(id, userId.Value);
        if (!result)
        {
            return NotFound(new { message = "通知不存在" });
        }

        return Ok(new { message = "已标记为已读" });
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "认证信息无效" });
        }

        await _notificationService.MarkAllAsReadAsync(userId.Value);
        return Ok(new { message = "已全部标记为已读" });
    }
}
