using STEM.Core.Entities.Projects;

namespace STEM.Core.Repository;

public interface IResubmitRequestRepository : IRepository<ResubmitRequest>
{
    Task<ResubmitRequest?> GetPendingAsync(
        int assignmentId,
        int studentId,
        CancellationToken cancellationToken = default);

    Task<ResubmitRequest?> GetByIdWithDetailsAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<ResubmitRequest>> GetFilteredAsync(
        int? assignmentId,
        string? status,
        int? studentId,
        int? teacherId,
        int? schoolId,
        CancellationToken cancellationToken = default);
}
