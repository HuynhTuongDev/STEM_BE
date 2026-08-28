using STEM.Application.Interfaces;
using STEM.Core.Entities.Common;
using STEM.Core.Repository;

namespace STEM.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;

    public NotificationService(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task SendAsync(
        int userId,
        string title,
        string content,
        NotificationType type,
        CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Content = content,
            Type = type,
            IsRead = false
        };

        await _notificationRepository.AddAsync(notification, cancellationToken);
        await _notificationRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task SendToManyAsync(
        IEnumerable<int> userIds,
        string title,
        string content,
        NotificationType type,
        CancellationToken cancellationToken = default)
    {
        var notifications = userIds.Select(userId => new Notification
        {
            UserId = userId,
            Title = title,
            Content = content,
            Type = type,
            IsRead = false
        }).ToList();

        if (notifications.Count == 0) return;

        await _notificationRepository.AddRangeAsync(notifications);
        await _notificationRepository.SaveChangesAsync(cancellationToken);
    }
}
