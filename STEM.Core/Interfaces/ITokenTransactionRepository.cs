using STEM.Core.Entities.Payments;
using STEM.Core.Repository;

namespace STEM.Core.Interfaces;

public interface ITokenTransactionRepository : IRepository<TokenTransaction>
{
    Task<IEnumerable<TokenTransaction>> GetByAccountIdAsync(int accountId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<TokenTransaction>> GetBySchoolIdAsync(int schoolId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetCountByAccountIdAsync(int accountId, CancellationToken cancellationToken = default);
}
