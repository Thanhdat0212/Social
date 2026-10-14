using Application.Common;
using Application.DTOs.Notifications;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Exceptions;
using Infrastructure.Realtime;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHubContext<SocialHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IHubContext<SocialHub> hubContext,
        ILogger<NotificationService> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<NotificationDto>> GetMyNotificationsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var notifications = await _unitOfWork.Notifications.GetByUserIdAsync(userId, page, pageSize, cancellationToken);

        return notifications.Select(n => new NotificationDto
        {
            Id = n.Id,
            Type = n.Type,
            Title = n.Title,
            Body = n.Body,
            IsRead = n.IsRead,
            ReadAtUtc = n.ReadAtUtc,
            DataJson = n.DataJson,
            CreatedAtUtc = n.CreatedAtUtc
        }).ToList();
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        return await _unitOfWork.Notifications.GetUnreadCountAsync(userId, cancellationToken);
    }

    public async Task<bool> MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var notification = await _unitOfWork.Notifications.GetByIdAndUserIdAsync(notificationId, userId, cancellationToken);

        if (notification == null)
        {
            throw new NotFoundException("Notification", notificationId);
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAtUtc = DateTime.UtcNow;
            notification.UpdatedAtUtc = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public async Task<bool> MarkAllAsReadAsync(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        await _unitOfWork.Notifications.MarkAllAsReadAsync(userId, cancellationToken);
        return true;
    }

    public async Task<NotificationDto> SendNotificationAsync(CreateNotificationDto dto, CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            UserId = dto.UserId,
            Type = dto.Type,
            Title = dto.Title,
            Body = dto.Body,
            DataJson = dto.DataJson,
            IsRead = false,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await _unitOfWork.Notifications.AddAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var resultDto = new NotificationDto
        {
            Id = notification.Id,
            Type = notification.Type,
            Title = notification.Title,
            Body = notification.Body,
            IsRead = notification.IsRead,
            DataJson = notification.DataJson,
            CreatedAtUtc = notification.CreatedAtUtc
        };

        // Phát thông báo Realtime qua SignalR tới Group cá nhân của User
        try
        {
            await _hubContext.Clients.Group($"user_{dto.UserId}")
                .SendAsync("ReceivePersistentNotification", resultDto, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể gửi realtime thông báo tới user_{UserId}: {Message}", dto.UserId, ex.Message);
        }

        return resultDto;
    }

    private Guid GetCurrentUserId()
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");
        }

        return _currentUserService.UserId.Value;
    }
}
