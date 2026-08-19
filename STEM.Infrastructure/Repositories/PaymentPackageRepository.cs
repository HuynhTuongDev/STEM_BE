using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Payments;
using STEM.Core.Interfaces;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Repositories;

public class PaymentPackageRepository : Repository<PaymentPackage>, IPaymentPackageRepository
{
    public PaymentPackageRepository(StemDbContext context) : base(context)
    {
    }

    public async Task<PaymentPackage?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Payments)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<PaymentPackage>> GetActivePackagesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.IsActive)
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.Price)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PaymentPackage>> GetAllPackagesAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();
        
        if (!includeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

        return await query
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.Price)
            .ToListAsync(cancellationToken);
    }
}
