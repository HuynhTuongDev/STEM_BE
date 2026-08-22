namespace STEM.Application.Interfaces;

/// <summary>
/// Business/security audit trail writer. Never throws — a write failure here
/// must not fail the business operation that triggered it (see
/// docs/ERD_IMPLEMENTATION_MAPPING.md-adjacent audit-log decision notes:
/// availability over audit atomicity for this capstone project). Implementers
/// log their own failures via ILogger instead of propagating.
/// </summary>
public interface ISystemLogService
{
    Task WriteAsync(
        string level,
        string action,
        int? actorUserId,
        string? actorRole,
        string? entityType,
        string? entityId,
        string description,
        object? metadata = null,
        CancellationToken cancellationToken = default);
}
