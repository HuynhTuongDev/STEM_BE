using Microsoft.Extensions.Logging;
using STEM.Application.DTOs.Payments;
using STEM.Core.Interfaces;

namespace STEM.Application.UseCases.Payments;

public class UpdatePackageHandler
{
    private readonly IPaymentPackageRepository _packageRepository;
    private readonly ILogger<UpdatePackageHandler> _logger;

    public UpdatePackageHandler(
        IPaymentPackageRepository packageRepository,
        ILogger<UpdatePackageHandler> logger)
    {
        _packageRepository = packageRepository;
        _logger = logger;
    }

    public async Task<PackageDto?> Handle(
        int packageId,
        UpdatePackageRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var package = await _packageRepository.GetByIdAsync(packageId, cancellationToken);
            if (package == null)
            {
                _logger.LogWarning("Package not found: {PackageId}", packageId);
                return null;
            }

            package.Name = request.Name ?? package.Name;
            package.Description = request.Description ?? package.Description;
            package.Price = request.Price ?? package.Price;
            package.Currency = request.Currency ?? package.Currency;
            package.TokenAmount = request.TokenAmount ?? package.TokenAmount;
            package.StudentLimit = request.StudentLimit ?? package.StudentLimit;
            package.IsActive = request.IsActive ?? package.IsActive;
            package.IsFeatured = request.IsFeatured ?? package.IsFeatured;
            package.Features = request.Features ?? package.Features;
            package.DisplayOrder = request.DisplayOrder ?? package.DisplayOrder;
            if (request.ExpiresAt.HasValue)
                package.ExpiresAt = request.ExpiresAt.Value;

            _packageRepository.Update(package);
            await _packageRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Package updated: {PackageId} - {PackageName}", package.Id, package.Name);

            return new PackageDto(
                package.Id,
                package.Name,
                package.Description,
                package.Price,
                package.Currency,
                package.TokenAmount,
                package.StudentLimit,
                package.IsActive,
                package.IsFeatured,
                package.Features,
                package.ExpiresAt
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating package {PackageId}", packageId);
            return null;
        }
    }
}

public class UpdatePackageRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public string? Currency { get; set; }
    public int? TokenAmount { get; set; }
    public int? StudentLimit { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsFeatured { get; set; }
    public string? Features { get; set; }
    public int? DisplayOrder { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
