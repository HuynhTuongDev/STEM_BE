using STEM.Application.Dtos.Notifications;
using STEM.Core.Entities.Common;
using STEM.Core.Entities.Users;
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
        var notifications = await _notificationRepository.GetByUserIdAsync(userId.ToString(), 0, 100, cancellationToken: cancellationToken);
        return notifications.Select(n => new NotificationResponse
        {
            Id = n.Id,
            UserId = n.UserId,
            Title = n.Title,
            Content = n.Content,
            Type = n.Type.ToString(),
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt
        });
    }

    public async Task<bool> MarkAsRead(int id, int currentUserId, string currentUserRole, CancellationToken cancellationToken = default)
    {
        var notification = await _notificationRepository.GetByIdAsync(id, cancellationToken);
        if (notification == null) return false;

        if (currentUserRole != RoleNames.SchoolAdministrator && notification.UserId != currentUserId)
        {
            throw new UnauthorizedAccessException("You do not have permission to mark this notification as read.");
        }

        notification.IsRead = true;
        notification.UpdatedAt = DateTime.UtcNow;
        _notificationRepository.Update(notification);
        await _notificationRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteNotification(int id, int currentUserId, string currentUserRole, CancellationToken cancellationToken = default)
    {
        var notification = await _notificationRepository.GetByIdAsync(id, cancellationToken);
        if (notification == null) return false;

        if (currentUserRole != RoleNames.SchoolAdministrator && notification.UserId != currentUserId)
        {
            throw new UnauthorizedAccessException("You do not have permission to delete this notification.");
        }

        _notificationRepository.Delete(notification);
        await _notificationRepository.SaveChangesAsync(cancellationToken);
        return true;
    }
}
