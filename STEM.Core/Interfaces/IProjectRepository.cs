using STEM.Core.Entities.Projects;

namespace STEM.Core.Repository;

public interface IProjectRepository : IRepository<Project>
{
    Task<IEnumerable<Project>> GetByClassIdAsync(int classId, CancellationToken cancellationToken = default);
}