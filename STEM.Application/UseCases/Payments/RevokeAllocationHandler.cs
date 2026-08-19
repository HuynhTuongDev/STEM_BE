using Microsoft.Extensions.Logging;
using STEM.Core.Interfaces;

namespace STEM.Application.UseCases.Payments;

public class RevokeAllocationHandler
{
    private readonly ITokenAccountRepository _accountRepository;
    private readonly ITokenAllocationRepository _allocationRepository;
    private readonly ITokenTransactionRepository _transactionRepository;
    private readonly ILogger<RevokeAllocationHandler> _logger;

    public RevokeAllocationHandler(
        ITokenAccountRepository accountRepository,
        ITokenAllocationRepository allocationRepository,
        ITokenTransactionRepository transactionRepository,
        ILogger<RevokeAllocationHandler> logger)
    {
        _accountRepository = accountRepository;
        _allocationRepository = allocationRepository;
        _transactionRepository = transactionRepository;
        _logger = logger;
    }

    public async Task<bool> Handle(
        int allocationId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var allocation = await _allocationRepository.GetByIdWithUserAsync(allocationId, cancellationToken);
            if (allocation == null || !allocation.IsActive)
            {
                _logger.LogWarning("Allocation not found or already revoked: {AllocationId}", allocationId);
                return false;
            }

            var unusedTokens = allocation.AllocatedTokens - allocation.UsedTokens;
            
            allocation.IsActive = false;
            allocation.RevokedAt = DateTime.UtcNow;
            allocation.RevocationReason = reason;
            
            _allocationRepository.Update(allocation);

            if (unusedTokens > 0 && allocation.AccountId > 0)
            {
                var account = await _accountRepository.GetByIdAsync(allocation.AccountId, cancellationToken);
                if (account != null)
                {
                    account.TokensRemaining += unusedTokens;
                    _accountRepository.Update(account);

                    var transaction = new Core.Entities.Payments.TokenTransaction
                    {
                        AccountId = account.Id,
                        Type = Core.Entities.Payments.TransactionType.Revocation,
                        Quantity = unusedTokens,
                        BalanceAfter = account.TokensRemaining,
                        Description = $"Revoked {unusedTokens} tokens from user {allocation.User?.FullName}. Reason: {reason ?? "No reason provided"}",
                        ReferenceId = allocationId.ToString()
                    };
                    await _transactionRepository.AddAsync(transaction, cancellationToken);
                    await _transactionRepository.SaveChangesAsync(cancellationToken);
                }
            }

            await _allocationRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Allocation revoked: {AllocationId}, Tokens returned: {Tokens}", allocationId, unusedTokens);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking allocation {AllocationId}", allocationId);
            return false;
        }
    }
}
