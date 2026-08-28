using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Common;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Repositories;

public class NotificationRepository : Repository<Notification>, INotificationRepository
{
    public NotificationRepository(StemDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Notification>> GetByUserIdAsync(
        string userId,
        int skip,
        int take,
        NotificationType? type = null,
        NotificationStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (int.TryParse(userId, out var userIdInt))
        {
            query = query.Where(n => n.UserId == userIdInt);
        }

        if (type.HasValue)
        {
            var typeValue = type.Value;
            query = query.Where(n => n.Type == typeValue);
        }

        if (status.HasValue)
        {
            var statusValue = status.Value;
            query = query.Where(n => statusValue == NotificationStatus.Read ? n.IsRead : !n.IsRead);
        }

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsReadAsync(int id, CancellationToken cancellationToken = default)
    {
        var notification = await _dbSet.FindAsync(new object[] { id }, cancellationToken);
        if (notification != null)
        {
            notification.IsRead = true;
            notification.UpdatedAt = DateTime.UtcNow;
            _dbSet.Update(notification);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (int.TryParse(userId, out var userIdInt))
        {
            var unreadNotifications = await _dbSet
                .Where(n => n.UserId == userIdInt && !n.IsRead)
                .ToListAsync(cancellationToken);

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (int.TryParse(userId, out var userIdInt))
        {
            return await _dbSet
                .CountAsync(n => n.UserId == userIdInt && !n.IsRead, cancellationToken);
        }
        return 0;
    }
}
