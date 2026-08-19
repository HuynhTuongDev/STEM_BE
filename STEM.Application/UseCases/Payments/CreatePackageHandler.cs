using Microsoft.Extensions.Logging;
using STEM.Application.DTOs.Payments;
using STEM.Core.Entities.Payments;
using STEM.Core.Interfaces;

namespace STEM.Application.UseCases.Payments;

public class CreatePackageHandler
{
    private readonly IPaymentPackageRepository _packageRepository;
    private readonly ILogger<CreatePackageHandler> _logger;

    public CreatePackageHandler(
        IPaymentPackageRepository packageRepository,
        ILogger<CreatePackageHandler> logger)
    {
        _packageRepository = packageRepository;
        _logger = logger;
    }

    public async Task<PackageDto?> Handle(
        CreatePackageRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var expiresAt = request.ExpiresAt ?? DateTime.UtcNow.AddMonths(1).Date.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);
            
            var package = new PaymentPackage
            {
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                Currency = request.Currency ?? "VND",
                TokenAmount = request.TokenAmount,
                StudentLimit = request.StudentLimit,
                IsActive = request.IsActive,
                IsFeatured = request.IsFeatured,
                Features = request.Features,
                DisplayOrder = request.DisplayOrder,
                ExpiresAt = expiresAt
            };

            await _packageRepository.AddAsync(package, cancellationToken);
            await _packageRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Package created: {PackageId} - {PackageName}", package.Id, package.Name);

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
            _logger.LogError(ex, "Error creating package");
            return null;
        }
    }
}

public class CreatePackageRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "VND";
    public int TokenAmount { get; set; }
    public int StudentLimit { get; set; }  // Số học sinh tối đa
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }
    public string? Features { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
