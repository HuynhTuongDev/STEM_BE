using STEM.Core.Entities.Common;

namespace STEM.Application.Interfaces;

public interface INotificationService
{
    Task SendAsync(int userId, string title, string content, NotificationType type, CancellationToken cancellationToken = default);
    Task SendToManyAsync(IEnumerable<int> userIds, string title, string content, NotificationType type, CancellationToken cancellationToken = default);
}
