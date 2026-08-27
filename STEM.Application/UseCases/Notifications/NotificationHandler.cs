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

    public async Task<IEnumerable<NotificationResponse>> GetNotificationsByUserId(int userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var skip = (page - 1) * pageSize;
        var notifications = await _notificationRepository.GetByUserIdAsync(userId.ToString(), skip, pageSize, cancellationToken: cancellationToken);
        
        var response = notifications.Select(n => new NotificationResponse
        {
            Id = n.Id,
            UserId = n.UserId,
            Title = n.Title,
            Content = n.Content,
            Type = n.Type.ToString(),
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt
        }).ToList();
        
        Console.WriteLine($"[DEBUG] GetNotificationsByUserId: userId={userId}, found {response.Count} notifications");
        return response;
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

    public async Task MarkAllAsRead(int userId, CancellationToken cancellationToken = default)
    {
        await _notificationRepository.MarkAllAsReadAsync(userId.ToString(), cancellationToken);
    }
}
