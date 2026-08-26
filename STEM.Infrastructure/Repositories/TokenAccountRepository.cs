using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Payments;
using STEM.Core.Interfaces;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Repositories;

public class TokenAccountRepository : Repository<TokenAccount>, ITokenAccountRepository
{
    public TokenAccountRepository(StemDbContext context) : base(context)
    {
    }

    public async Task<TokenAccount?> GetBySchoolIdAsync(int schoolId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(a => a.School)
            .Include(a => a.Allocations)
            .FirstOrDefaultAsync(a => a.SchoolId == schoolId, cancellationToken);
    }

    public async Task<TokenAccount> GetOrCreateBySchoolIdAsync(int schoolId, CancellationToken cancellationToken = default)
    {
        var account = await _dbSet.FirstOrDefaultAsync(a => a.SchoolId == schoolId, cancellationToken);
        
        if (account == null)
        {
            account = new TokenAccount
            {
                SchoolId = schoolId,
                TotalTokensPurchased = 0,
                TokensRemaining = 0,
                TokensUsed = 0
            };
            await _dbSet.AddAsync(account, cancellationToken);
            await SaveChangesAsync(cancellationToken);
        }

        return account;
    }

    public async Task UpdateBalanceAsync(int schoolId, int tokensToAdd, CancellationToken cancellationToken = default)
    {
        var account = await GetOrCreateBySchoolIdAsync(schoolId, cancellationToken);
        account.TokensRemaining += tokensToAdd;
        account.TotalTokensPurchased += tokensToAdd;
        account.LastPurchaseAt = DateTime.UtcNow;
        Update(account);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task DecrementBalanceAsync(int schoolId, int tokensToUse, CancellationToken cancellationToken = default)
    {
        var account = await GetOrCreateBySchoolIdAsync(schoolId, cancellationToken);
        if (account.TokensRemaining >= tokensToUse)
        {
            account.TokensRemaining -= tokensToUse;
            account.TokensUsed += tokensToUse;
            Update(account);
            await SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> HasEnoughTokensAsync(int schoolId, int tokensRequired, CancellationToken cancellationToken = default)
    {
        var account = await GetBySchoolIdAsync(schoolId, cancellationToken);
        return account != null && account.TokensRemaining >= tokensRequired;
    }
}
