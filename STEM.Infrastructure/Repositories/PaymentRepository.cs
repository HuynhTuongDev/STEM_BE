using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Payments;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Repositories;

public class PaymentRepository : Repository<Payment>, IPaymentRepository
{
    public PaymentRepository(StemDbContext context) : base(context)
    {
    }

    public async Task<Payment?> GetByTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Package)
            .Include(p => p.Buyer)
            .Include(p => p.Seller)
            .FirstOrDefaultAsync(p => p.TransactionId == transactionId, cancellationToken);
    }

    public async Task<(IEnumerable<Payment> Payments, int TotalCount)> GetBySchoolAsync(int schoolId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(p => p.Package)
            .Where(p => p.Buyer!.SchoolId == schoolId || p.Seller!.SchoolId == schoolId)
            .OrderByDescending(p => p.CreatedAt)
            .AsQueryable();

        var totalCount = await query.CountAsync(cancellationToken);

        var payments = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (payments, totalCount);
    }

    public async Task<IEnumerable<Payment>> GetByBuyerAsync(int buyerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Package)
            .Where(p => p.BuyerId == buyerId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}

public class PaymentPackageRepository : Repository<PaymentPackage>, IPaymentPackageRepository
{
    public PaymentPackageRepository(StemDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<PaymentPackage>> GetActivePackagesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Price)
            .ToListAsync(cancellationToken);
    }

    public async Task<PaymentPackage?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }
}

public class TokenAccountRepository : Repository<TokenAccount>, ITokenAccountRepository
{
    public TokenAccountRepository(StemDbContext context) : base(context)
    {
    }

    public async Task<TokenAccount?> GetBySchoolIdAsync(int schoolId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(t => t.SchoolId == schoolId, cancellationToken);
    }

    public async Task<TokenAccount> GetOrCreateAsync(int schoolId, CancellationToken cancellationToken = default)
    {
        var account = await GetBySchoolIdAsync(schoolId, cancellationToken);
        if (account == null)
        {
            account = new TokenAccount
            {
                SchoolId = schoolId,
                TotalTokensPurchased = 0,
                TokensRemaining = 0,
                TokensUsed = 0
            };
            await AddAsync(account, cancellationToken);
        }
        return account;
    }
}

public class TokenTransactionRepository : Repository<TokenTransaction>, ITokenTransactionRepository
{
    public TokenTransactionRepository(StemDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<TokenTransaction>> GetBySchoolAsync(int schoolId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.SchoolId == schoolId)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TokenTransaction>> GetByPaymentAsync(int paymentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.PaymentId == paymentId)
            .ToListAsync(cancellationToken);
    }
}
