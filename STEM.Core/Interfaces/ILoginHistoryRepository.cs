using STEM.Core.Entities.Users;

namespace STEM.Core.Repository;

public interface ILoginHistoryRepository : IRepository<LoginHistory>
{
    Task<IEnumerable<LoginHistory>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<LoginHistory>> GetByUserIdAndSchoolIdAsync(int userId, int schoolId, CancellationToken cancellationToken = default);
}