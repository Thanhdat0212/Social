using Application.DTOs.Notifications;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

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

    /// <summary>
    /// Lấy danh sách thông báo của người dùng hiện tại (phân trang)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var notifications = await _notificationService.GetMyNotificationsAsync(page, pageSize, cancellationToken);
        return Ok(notifications);
    }

    /// <summary>
    /// Đếm số lượng thông báo chưa đọc của người dùng
    /// </summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken = default)
    {
        var count = await _notificationService.GetUnreadCountAsync(cancellationToken);
        return Ok(new { unreadCount = count });
    }

    /// <summary>
    /// Đánh dấu một thông báo đã đọc
    /// </summary>
    [HttpPatch("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken = default)
    {
        await _notificationService.MarkAsReadAsync(id, cancellationToken);
        return Ok(new { message = "Đã đánh dấu thông báo là đã đọc." });
    }

    /// <summary>
    /// Đánh dấu tất cả thông báo của người dùng là đã đọc
    /// </summary>
    [HttpPatch("read-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken = default)
    {
        await _notificationService.MarkAllAsReadAsync(cancellationToken);
        return Ok(new { message = "Đã đánh dấu tất cả thông báo là đã đọc." });
    }
}
