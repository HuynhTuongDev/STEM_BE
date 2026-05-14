using STEM.Core.Entities.Projects;

namespace STEM.Core.Repository;

public interface IAssignmentRepository : IRepository<Assignment>
{
    Task<IEnumerable<Assignment>> GetByCourseIdAsync(int courseId, CancellationToken cancellationToken = default);
}