using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Payments;
using STEM.Core.Interfaces;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Repositories;

public class PaymentRepository : Repository<Payment>, IPaymentRepository
{
    public PaymentRepository(StemDbContext context) : base(context)
    {
    }

    public async Task<Payment?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Package)
            .Include(p => p.School)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Payment?> GetByTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Package)
            .Include(p => p.School)
            .FirstOrDefaultAsync(p => p.TransactionId == transactionId, cancellationToken);
    }

    public async Task<Payment?> GetByPaymentLinkIdAsync(string paymentLinkId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Package)
            .FirstOrDefaultAsync(p => p.PaymentLinkId == paymentLinkId, cancellationToken);
    }

    public async Task<Payment?> GetByOrderCodeAsync(long orderCode, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Package)
            .Include(p => p.School)
            .FirstOrDefaultAsync(p => p.OrderCode == orderCode, cancellationToken);
    }

    public async Task<IEnumerable<Payment>> GetBySchoolIdAsync(int schoolId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Package)
            .Where(p => p.SchoolId == schoolId)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountBySchoolIdAsync(int schoolId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(p => p.SchoolId == schoolId, cancellationToken);
    }

    public async Task<IEnumerable<Payment>> GetPendingPaymentsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.Status == PaymentStatus.Pending)
            .ToListAsync(cancellationToken);
    }
}
