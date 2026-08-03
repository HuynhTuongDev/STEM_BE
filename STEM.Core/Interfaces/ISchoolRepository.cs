using STEM.Core.Entities.Schools;

namespace STEM.Core.Repository;

public interface ISchoolRepository : IRepository<School>
{
    Task<bool> ExistsAsync(int? schoolId, CancellationToken cancellationToken = default);
}
