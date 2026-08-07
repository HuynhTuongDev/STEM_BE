using STEM.Core.Entities.Projects;

namespace STEM.Core.Repository;

public interface ISubmissionRepository : IRepository<Submission>
{
    Task<IEnumerable<Submission>> GetByAssignmentIdAsync(int assignmentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Submission>> GetByStudentIdAsync(int studentId, CancellationToken cancellationToken = default);

    Task<Submission?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);

    Task<(IEnumerable<Submission> Submissions, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        int? assignmentId,
        int? classId,
        int? studentId,
        int? schoolId,
        int? teacherId,
        CancellationToken cancellationToken = default);
}
