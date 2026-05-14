using STEM.Core.Entities.Quizzes;

namespace STEM.Core.Repository;

public interface ICertificateRepository : IRepository<Certificate>
{
    Task<IEnumerable<Certificate>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
}