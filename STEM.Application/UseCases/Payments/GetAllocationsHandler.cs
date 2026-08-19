using Microsoft.Extensions.Logging;
using STEM.Application.DTOs.Payments;
using STEM.Core.Interfaces;

namespace STEM.Application.UseCases.Payments;

public class GetAllocationsHandler
{
    private readonly ITokenAccountRepository _accountRepository;
    private readonly ITokenAllocationRepository _allocationRepository;
    private readonly ILogger<GetAllocationsHandler> _logger;

    public GetAllocationsHandler(
        ITokenAccountRepository accountRepository,
        ITokenAllocationRepository allocationRepository,
        ILogger<GetAllocationsHandler> logger)
    {
        _accountRepository = accountRepository;
        _allocationRepository = allocationRepository;
        _logger = logger;
    }

    public async Task<TokenAllocationListResponse> Handle(
        int schoolId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var account = await _accountRepository.GetBySchoolIdAsync(schoolId, cancellationToken);
            if (account == null)
            {
                return new TokenAllocationListResponse(new List<TokenAllocationDto>(), 0, page, pageSize);
            }

            var allocations = await _allocationRepository.GetByAccountIdAsync(account.Id, page, pageSize, cancellationToken);
            var total = await _allocationRepository.GetCountByAccountIdAsync(account.Id, cancellationToken);

            var allocationDtos = allocations.Select(a => new TokenAllocationDto(
                a.Id,
                schoolId,
                a.UserId,
                a.User?.FullName ?? "Unknown",
                a.User?.Email ?? "Unknown",
                a.User?.Role?.Name ?? "Unknown",
                a.AllocatedTokens,
                a.UsedTokens,
                a.AllocatedTokens - a.UsedTokens,
                a.ExpiresAt,
                a.Notes,
                a.AllocatedByUserId,
                a.AllocatedByUser?.FullName ?? "System",
                a.CreatedAt
            )).ToList();

            return new TokenAllocationListResponse(allocationDtos, total, page, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting allocations for school {SchoolId}", schoolId);
            return new TokenAllocationListResponse(new List<TokenAllocationDto>(), 0, page, pageSize);
        }
    }
}
