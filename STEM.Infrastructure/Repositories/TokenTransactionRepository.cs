using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Payments;
using STEM.Core.Interfaces;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Repositories;

public class TokenTransactionRepository : Repository<TokenTransaction>, ITokenTransactionRepository
{
    private readonly StemDbContext _context;

    public TokenTransactionRepository(StemDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TokenTransaction>> GetByAccountIdAsync(int accountId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.AccountId == accountId)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TokenTransaction>> GetBySchoolIdAsync(int schoolId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var account = await _context.TokenAccounts
            .FirstOrDefaultAsync(a => a.SchoolId == schoolId, cancellationToken);

        if (account == null)
            return Enumerable.Empty<TokenTransaction>();

        return await _dbSet
            .Include(t => t.Payment)
            .Where(t => t.AccountId == account.Id)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountByAccountIdAsync(int accountId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(t => t.AccountId == accountId, cancellationToken);
    }
}
