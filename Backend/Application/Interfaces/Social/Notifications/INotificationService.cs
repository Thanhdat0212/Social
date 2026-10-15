using Application.DTOs.Notifications;

namespace Application.Interfaces;

public interface INotificationService
{
    Task<IReadOnlyList<NotificationDto>> GetMyNotificationsAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default);
    Task<bool> MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default);
    Task<bool> MarkAllAsReadAsync(CancellationToken cancellationToken = default);
    Task<NotificationDto> SendNotificationAsync(CreateNotificationDto dto, CancellationToken cancellationToken = default);
}
