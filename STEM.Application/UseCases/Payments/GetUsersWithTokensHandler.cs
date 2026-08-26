using Microsoft.Extensions.Logging;
using STEM.Application.DTOs.Payments;
using STEM.Core.Interfaces;

namespace STEM.Application.UseCases.Payments;

public class GetUsersWithTokensHandler
{
    private readonly ITokenAccountRepository _accountRepository;
    private readonly ITokenAllocationRepository _allocationRepository;
    private readonly ILogger<GetUsersWithTokensHandler> _logger;

    public GetUsersWithTokensHandler(
        ITokenAccountRepository accountRepository,
        ITokenAllocationRepository allocationRepository,
        ILogger<GetUsersWithTokensHandler> logger)
    {
        _accountRepository = accountRepository;
        _allocationRepository = allocationRepository;
        _logger = logger;
    }

    public async Task<List<UserTokenInfoDto>> Handle(
        int schoolId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var account = await _accountRepository.GetBySchoolIdAsync(schoolId, cancellationToken);
            if (account == null)
            {
                return new List<UserTokenInfoDto>();
            }

            var allocations = await _allocationRepository.GetByAccountIdAsync(account.Id, 1, 1000, cancellationToken);

            return allocations
                .Where(a => a.User != null)
                .Select(a => new UserTokenInfoDto(
                    a.UserId,
                    a.User?.FullName ?? "Unknown",
                    a.User?.Email ?? "Unknown",
                    a.User?.Role?.Name ?? "Unknown",
                    a.AllocatedTokens,
                    a.UsedTokens,
                    a.AllocatedTokens - a.UsedTokens,
                    a.ExpiresAt,
                    null
                ))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users with tokens for school {SchoolId}", schoolId);
            return new List<UserTokenInfoDto>();
        }
    }
}
