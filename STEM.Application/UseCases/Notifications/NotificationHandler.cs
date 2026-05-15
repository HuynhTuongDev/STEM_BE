using STEM.Application.Dtos.Notifications;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Notifications;

public class NotificationHandler
{
    private readonly INotificationRepository _notificationRepository;

    public NotificationHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<IEnumerable<NotificationResponse>> GetNotificationsByUserId(int userId, CancellationToken cancellationToken = default)
    {
        var notifications = await _notificationRepository.GetByUserIdAsync(userId, cancellationToken);
        return notifications.Select(n => new NotificationResponse
        {
            Id = n.Id,
            UserId = n.UserId,
            Title = n.Title,
            Content = n.Content,
            Type = n.Type,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt
        });
    }

    public async Task MarkAsRead(int id, CancellationToken cancellationToken = default)
    {
        await _notificationRepository.MarkAsReadAsync(id, cancellationToken);
        await _notificationRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteNotification(int id, CancellationToken cancellationToken = default)
    {
        var notification = await _notificationRepository.GetByIdAsync(id, cancellationToken);
        if (notification != null)
        {
            _notificationRepository.Delete(notification);
            await _notificationRepository.SaveChangesAsync(cancellationToken);
        }
    }
}
