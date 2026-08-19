using Microsoft.Extensions.Logging;
using STEM.Application.DTOs.Payments;
using STEM.Core.Interfaces;

namespace STEM.Application.UseCases.Payments;

public class GetBalanceHandler
{
    private readonly ITokenAccountRepository _accountRepository;
    private readonly ITokenAllocationRepository _allocationRepository;
    private readonly ILogger<GetBalanceHandler> _logger;

    public GetBalanceHandler(
        ITokenAccountRepository accountRepository,
        ITokenAllocationRepository allocationRepository,
        ILogger<GetBalanceHandler> logger)
    {
        _accountRepository = accountRepository;
        _allocationRepository = allocationRepository;
        _logger = logger;
    }

    public async Task<TokenBalanceDto?> Handle(int schoolId, string schoolName, CancellationToken cancellationToken = default)
    {
        try
        {
            var account = await _accountRepository.GetBySchoolIdAsync(schoolId, cancellationToken);
            
            if (account == null)
            {
                return new TokenBalanceDto(
                    SchoolId: schoolId,
                    SchoolName: schoolName,
                    TotalTokensPurchased: 0,
                    TokensRemaining: 0,
                    TokensDistributed: 0,
                    TokensUsed: 0,
                    ExpiresAt: null,
                    LastPurchaseAt: null
                );
            }

            var allocatedTokens = await _allocationRepository.GetTotalAllocatedByAccountIdAsync(account.Id, cancellationToken);

            return new TokenBalanceDto(
                SchoolId: schoolId,
                SchoolName: schoolName,
                TotalTokensPurchased: account.TotalTokensPurchased,
                TokensRemaining: account.TokensRemaining,
                TokensDistributed: allocatedTokens,
                TokensUsed: account.TokensUsed,
                ExpiresAt: account.ExpiresAt,
                LastPurchaseAt: account.LastPurchaseAt
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting balance for school {SchoolId}", schoolId);
            return null;
        }
    }
}
