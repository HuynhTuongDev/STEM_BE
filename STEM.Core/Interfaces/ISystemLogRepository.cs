using STEM.Core.Entities.Common;

namespace STEM.Core.Repository;

/// <summary>
/// Deliberately NOT IRepository&lt;T&gt; — SystemLog is append-only, so there is
/// no Update/Delete here at all (not even at the persistence layer), matching
/// the "no UpdateSystemLog/DeleteSystemLog" business rule one level down.
/// </summary>
public interface ISystemLogRepository
{
    Task AddAsync(SystemLog log, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<SystemLog?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<(IEnumerable<SystemLog> Logs, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? action,
        string? level,
        int? actorUserId,
        string? entityType,
        string? entityId,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default);
}
