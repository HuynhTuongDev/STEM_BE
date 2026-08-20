using STEM.Core.Entities.Payments;
using STEM.Core.Repository;

namespace STEM.Core.Interfaces;

public interface IPaymentRepository : IRepository<Payment>
{
    Task<Payment?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<Payment?> GetByTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default);
    Task<Payment?> GetByPaymentLinkIdAsync(string paymentLinkId, CancellationToken cancellationToken = default);
    Task<Payment?> GetByOrderCodeAsync(long orderCode, CancellationToken cancellationToken = default);
    Task<IEnumerable<Payment>> GetBySchoolIdAsync(int schoolId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetCountBySchoolIdAsync(int schoolId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Payment>> GetPendingPaymentsAsync(CancellationToken cancellationToken = default);
}
