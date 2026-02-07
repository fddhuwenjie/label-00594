using Microsoft.AspNetCore.Mvc;
using PurchaseApproval.Services;

namespace PurchaseApproval.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// 获取通知列表
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromHeader(Name = "X-User-Id")] Guid userId, [FromQuery] bool? unreadOnly)
    {
        if (userId == Guid.Empty)
        {
            return BadRequest(new { message = "请先登录" });
        }

        var notifications = await _notificationService.GetByUserIdAsync(userId, unreadOnly);
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

    /// <summary>
    /// 获取未读数量
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount([FromHeader(Name = "X-User-Id")] Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return BadRequest(new { message = "请先登录" });
        }

        var count = await _notificationService.GetUnreadCountAsync(userId);
        return Ok(new { count });
    }

    /// <summary>
    /// 标记为已读
    /// </summary>
    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var result = await _notificationService.MarkAsReadAsync(id);
        if (!result)
        {
            return NotFound(new { message = "通知不存在" });
        }
        return Ok(new { message = "已标记为已读" });
    }

    /// <summary>
    /// 全部标记为已读
    /// </summary>
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead([FromHeader(Name = "X-User-Id")] Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return BadRequest(new { message = "请先登录" });
        }

        await _notificationService.MarkAllAsReadAsync(userId);
        return Ok(new { message = "已全部标记为已读" });
    }
}
