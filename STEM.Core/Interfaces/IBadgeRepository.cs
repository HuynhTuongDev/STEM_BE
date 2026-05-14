using STEM.Core.Entities.Quizzes;

namespace STEM.Core.Repository;

public interface IBadgeRepository : IRepository<Badge>
{
    Task<IEnumerable<Badge>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
}