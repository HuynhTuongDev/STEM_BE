using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Common;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Repositories;

public class SystemLogRepository : ISystemLogRepository
{
    private readonly StemDbContext _context;

    public SystemLogRepository(StemDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(SystemLog log, CancellationToken cancellationToken = default)
    {
        await _context.SystemLogs.AddAsync(log, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<SystemLog?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.SystemLogs.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<(IEnumerable<SystemLog> Logs, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? action,
        string? level,
        int? actorUserId,
        string? entityType,
        string? entityId,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SystemLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(l => l.Action == action);

        if (!string.IsNullOrWhiteSpace(level))
            query = query.Where(l => l.Level == level);

        if (actorUserId.HasValue)
            query = query.Where(l => l.ActorUserId == actorUserId.Value);

        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(l => l.EntityType == entityType);

        if (!string.IsNullOrWhiteSpace(entityId))
            query = query.Where(l => l.EntityId == entityId);

        if (from.HasValue)
            query = query.Where(l => l.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(l => l.CreatedAt <= to.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var logs = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (logs, totalCount);
    }
}
