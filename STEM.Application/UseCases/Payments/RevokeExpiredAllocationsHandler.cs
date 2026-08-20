using Microsoft.Extensions.Logging;
using STEM.Core.Entities.Payments;
using STEM.Core.Interfaces;

namespace STEM.Application.UseCases.Payments;

public class RevokeExpiredAllocationsHandler
{
    private readonly ITokenAllocationRepository _allocationRepository;
    private readonly ITokenAccountRepository _accountRepository;
    private readonly ITokenTransactionRepository _transactionRepository;
    private readonly ILogger<RevokeExpiredAllocationsHandler> _logger;

    public RevokeExpiredAllocationsHandler(
        ITokenAllocationRepository allocationRepository,
        ITokenAccountRepository accountRepository,
        ITokenTransactionRepository transactionRepository,
        ILogger<RevokeExpiredAllocationsHandler> logger)
    {
        _allocationRepository = allocationRepository;
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _logger = logger;
    }

    public async Task<RevokeResult> Handle(CancellationToken cancellationToken = default)
    {
        var result = new RevokeResult();

        try
        {
            var expiredAllocations = await _allocationRepository.GetExpiredAllocationsAsync(cancellationToken);
            var expiredList = expiredAllocations.ToList();

            if (!expiredList.Any())
            {
                _logger.LogInformation("No expired allocations to revoke");
                result.Message = "No expired allocations found";
                return result;
            }

            foreach (var allocation in expiredList)
            {
                try
                {
                    await RevokeSingleAllocation(allocation, "Expired", cancellationToken);
                    result.SuccessCount++;
                    result.TotalTokensReturned += allocation.AllocatedTokens - allocation.UsedTokens;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to revoke allocation {AllocationId}", allocation.Id);
                    result.FailedCount++;
                    result.Errors.Add($"Allocation {allocation.Id}: {ex.Message}");
                }
            }

            result.Message = $"Revoked {result.SuccessCount} allocations, returned {result.TotalTokensReturned} tokens";
            _logger.LogInformation(result.Message);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking expired allocations");
            result.Message = $"Error: {ex.Message}";
            return result;
        }
    }

    public async Task<bool> RevokeSingleAllocation(
        TokenAllocation allocation,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var remainingTokens = allocation.AllocatedTokens - allocation.UsedTokens;

        if (remainingTokens <= 0)
        {
            allocation.IsActive = false;
            allocation.ExpiresAt = DateTime.UtcNow;
            _allocationRepository.Update(allocation);
            await _allocationRepository.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Allocation {AllocationId} deactivated (no remaining tokens)", allocation.Id);
            return true;
        }

        var account = await _accountRepository.GetByIdAsync(allocation.AccountId, cancellationToken);
        if (account == null)
        {
            _logger.LogWarning("Account {AccountId} not found for allocation {AllocationId}", 
                allocation.AccountId, allocation.Id);
            return false;
        }

        account.TokensRemaining += remainingTokens;
        _accountRepository.Update(account);

        allocation.IsActive = false;
        allocation.RevokedAt = DateTime.UtcNow;
        allocation.RevocationReason = reason;
        _allocationRepository.Update(allocation);

        var transaction = new TokenTransaction
        {
            AccountId = account.Id,
            Type = TransactionType.Revocation,
            Quantity = remainingTokens,
            BalanceAfter = account.TokensRemaining,
            Description = $"Token revoked from user {allocation.User?.FullName ?? allocation.UserId.ToString()}. Reason: {reason}. Remaining: {remainingTokens}",
            ReferenceId = allocation.Id.ToString()
        };
        await _transactionRepository.AddAsync(transaction, cancellationToken);

        await _accountRepository.SaveChangesAsync(cancellationToken);
        await _allocationRepository.SaveChangesAsync(cancellationToken);
        await _transactionRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Revoked {Tokens} tokens from allocation {AllocationId} to account {AccountId}", 
            remainingTokens, allocation.Id, account.Id);

        return true;
    }
}

public class RevokeResult
{
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public int TotalTokensReturned { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
}
