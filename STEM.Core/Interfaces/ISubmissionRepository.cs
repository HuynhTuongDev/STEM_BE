using STEM.Core.Entities.Projects;

namespace STEM.Core.Repository;

public interface ISubmissionRepository : IRepository<Submission>
{
    Task<IEnumerable<Submission>> GetByAssignmentIdAsync(int assignmentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Submission>> GetByStudentIdAsync(int studentId, CancellationToken cancellationToken = default);
}