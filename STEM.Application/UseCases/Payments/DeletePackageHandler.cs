using Microsoft.Extensions.Logging;
using STEM.Core.Interfaces;

namespace STEM.Application.UseCases.Payments;

public class DeletePackageHandler
{
    private readonly IPaymentPackageRepository _packageRepository;
    private readonly ILogger<DeletePackageHandler> _logger;

    public DeletePackageHandler(
        IPaymentPackageRepository packageRepository,
        ILogger<DeletePackageHandler> logger)
    {
        _packageRepository = packageRepository;
        _logger = logger;
    }

    public async Task<bool> Handle(int packageId, CancellationToken cancellationToken = default)
    {
        try
        {
            var package = await _packageRepository.GetByIdAsync(packageId, cancellationToken);
            if (package == null)
            {
                _logger.LogWarning("Package not found: {PackageId}", packageId);
                return false;
            }

            _packageRepository.Delete(package);
            await _packageRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Package deleted: {PackageId}", packageId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting package {PackageId}", packageId);
            return false;
        }
    }
}
