using STEM.Core.Entities.Payments;

namespace STEM.Core.Repository;

public interface IPaymentRepository : IRepository<Payment>
{
    Task<Payment?> GetByTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Payment> Payments, int TotalCount)> GetBySchoolAsync(int schoolId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<Payment>> GetByBuyerAsync(int buyerId, CancellationToken cancellationToken = default);
}

public interface IPaymentPackageRepository : IRepository<PaymentPackage>
{
    Task<IEnumerable<PaymentPackage>> GetActivePackagesAsync(CancellationToken cancellationToken = default);
    Task<PaymentPackage?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);
}

public interface ITokenAccountRepository : IRepository<TokenAccount>
{
    Task<TokenAccount?> GetBySchoolIdAsync(int schoolId, CancellationToken cancellationToken = default);
    Task<TokenAccount> GetOrCreateAsync(int schoolId, CancellationToken cancellationToken = default);
}

public interface ITokenTransactionRepository : IRepository<TokenTransaction>
{
    Task<IEnumerable<TokenTransaction>> GetBySchoolAsync(int schoolId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<TokenTransaction>> GetByPaymentAsync(int paymentId, CancellationToken cancellationToken = default);
}
