using STEM.Core.Entities.Common;

namespace STEM.Core.Repository;

public interface INotificationRepository : IRepository<Notification>
{
    Task<IEnumerable<Notification>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default);
}