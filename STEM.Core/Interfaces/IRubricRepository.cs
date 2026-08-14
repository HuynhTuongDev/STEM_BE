using STEM.Core.Entities.Assessments;

namespace STEM.Core.Repository;

public interface IRubricRepository
{
    Task<Rubric?> GetByAssignmentIdAsync(int assignmentId, CancellationToken cancellationToken = default);
    Task AddAsync(Rubric rubric, CancellationToken cancellationToken = default);
    Task DeleteByAssignmentIdAsync(int assignmentId, CancellationToken cancellationToken = default);
}
