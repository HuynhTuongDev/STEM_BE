using STEM.Core.Entities.Payments;
using STEM.Core.Repository;

namespace STEM.Core.Interfaces;

public interface ITokenAccountRepository : IRepository<TokenAccount>
{
    Task<TokenAccount?> GetBySchoolIdAsync(int schoolId, CancellationToken cancellationToken = default);
    Task<TokenAccount> GetOrCreateBySchoolIdAsync(int schoolId, CancellationToken cancellationToken = default);
    Task UpdateBalanceAsync(int schoolId, int tokensToAdd, CancellationToken cancellationToken = default);
    Task DecrementBalanceAsync(int schoolId, int tokensToUse, CancellationToken cancellationToken = default);
    Task<bool> HasEnoughTokensAsync(int schoolId, int tokensRequired, CancellationToken cancellationToken = default);
}
