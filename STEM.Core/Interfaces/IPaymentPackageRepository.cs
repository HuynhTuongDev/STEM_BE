using STEM.Core.Entities.Payments;
using STEM.Core.Repository;

namespace STEM.Core.Interfaces;

public interface IPaymentPackageRepository : IRepository<PaymentPackage>
{
    Task<PaymentPackage?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<PaymentPackage>> GetActivePackagesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<PaymentPackage>> GetAllPackagesAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
}
