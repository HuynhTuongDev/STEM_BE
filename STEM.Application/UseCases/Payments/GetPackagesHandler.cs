using STEM.Application.DTOs.Payments;
using STEM.Core.Interfaces;

namespace STEM.Application.UseCases.Payments;

public class GetPackagesHandler
{
    private readonly IPaymentPackageRepository _packageRepository;

    public GetPackagesHandler(IPaymentPackageRepository packageRepository)
    {
        _packageRepository = packageRepository;
    }

    public async Task<GetPackagesResponse> Handle(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var packages = await _packageRepository.GetAllPackagesAsync(includeInactive, cancellationToken);
        
        var packageDtos = packages.Select(p => new PackageDto(
            p.Id,
            p.Name,
            p.Description,
            p.Price,
            p.Currency,
            p.TokenAmount,
            p.StudentLimit,
            p.IsActive,
            p.IsFeatured,
            p.Features,
            p.ExpiresAt
        )).ToList();

        return new GetPackagesResponse(packageDtos);
    }
}
