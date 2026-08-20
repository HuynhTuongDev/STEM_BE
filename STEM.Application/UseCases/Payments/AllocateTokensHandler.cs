using Microsoft.Extensions.Logging;
using STEM.Application.DTOs.Payments;
using STEM.Core.Entities.Payments;
using STEM.Core.Interfaces;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Payments;

public class AllocateTokensHandler
{
    private readonly ITokenAccountRepository _accountRepository;
    private readonly ITokenAllocationRepository _allocationRepository;
    private readonly ITokenTransactionRepository _transactionRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AllocateTokensHandler> _logger;

    public AllocateTokensHandler(
        ITokenAccountRepository accountRepository,
        ITokenAllocationRepository allocationRepository,
        ITokenTransactionRepository transactionRepository,
        IUserRepository userRepository,
        ILogger<AllocateTokensHandler> logger)
    {
        _accountRepository = accountRepository;
        _allocationRepository = allocationRepository;
        _transactionRepository = transactionRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<AllocateTokensResponse> Handle(
        AllocateTokensRequest request,
        int schoolId,
        int allocatedByUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var account = await _accountRepository.GetBySchoolIdAsync(schoolId, cancellationToken);
            if (account == null)
            {
                return new AllocateTokensResponse(
                    Success: false,
                    AllocationId: 0,
                    UserId: request.UserId,
                    UserName: "",
                    TokensAllocated: 0,
                    SchoolTokensRemaining: 0,
                    ExpiresAt: null,
                    ErrorMessage: "School account not found"
                );
            }

            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                return new AllocateTokensResponse(
                    Success: false,
                    AllocationId: 0,
                    UserId: request.UserId,
                    UserName: "",
                    TokensAllocated: 0,
                    SchoolTokensRemaining: account.TokensRemaining,
                    ExpiresAt: null,
                    ErrorMessage: "User not found"
                );
            }

            // Check if school has enough tokens
            if (account.TokensRemaining < request.Tokens)
            {
                return new AllocateTokensResponse(
                    Success: false,
                    AllocationId: 0,
                    UserId: request.UserId,
                    UserName: user.FullName,
                    TokensAllocated: 0,
                    SchoolTokensRemaining: account.TokensRemaining,
                    ExpiresAt: null,
                    ErrorMessage: $"Insufficient tokens. Available: {account.TokensRemaining}, Requested: {request.Tokens}"
                );
            }

            // Get existing allocation for this user
            var existingAllocation = await _allocationRepository.GetActiveAllocationAsync(account.Id, request.UserId, cancellationToken);
            
            int totalTokensForUser;
            DateTime? expiresAtUtc = request.ExpiresAt.HasValue 
                ? DateTime.SpecifyKind(request.ExpiresAt.Value, DateTimeKind.Utc)
                : (account.ExpiresAt.HasValue ? DateTime.SpecifyKind(account.ExpiresAt.Value, DateTimeKind.Utc) : null);

            if (existingAllocation != null)
            {
                // CUMULATIVE: Add to existing allocation
                totalTokensForUser = existingAllocation.AllocatedTokens + request.Tokens;
                existingAllocation.AllocatedTokens = totalTokensForUser;
                existingAllocation.Notes = request.Notes;
                existingAllocation.ExpiresAt = expiresAtUtc;
                existingAllocation.UpdatedAt = DateTime.UtcNow;
                _allocationRepository.Update(existingAllocation);
            }
            else
            {
                // Create new allocation
                var newAllocation = new TokenAllocation
                {
                    AccountId = account.Id,
                    UserId = request.UserId,
                    AllocatedTokens = request.Tokens,
                    UsedTokens = 0,
                    ExpiresAt = expiresAtUtc,
                    Notes = request.Notes,
                    AllocatedByUserId = allocatedByUserId,
                    IsActive = true
                };
                await _allocationRepository.AddAsync(newAllocation, cancellationToken);
                existingAllocation = newAllocation;
                totalTokensForUser = request.Tokens;
            }

            // Deduct tokens from school account
            account.TokensRemaining -= request.Tokens;
            _accountRepository.Update(account);

            // Create transaction record
            var transaction = new TokenTransaction
            {
                AccountId = account.Id,
                Type = TransactionType.Distribution,
                Quantity = -request.Tokens,
                BalanceAfter = account.TokensRemaining,
                Description = $"Allocated {request.Tokens} tokens to user {user.FullName}. User total: {totalTokensForUser}",
                ReferenceId = existingAllocation.Id.ToString()
            };
            await _transactionRepository.AddAsync(transaction, cancellationToken);

            // Save all changes
            await _accountRepository.SaveChangesAsync(cancellationToken);
            await _allocationRepository.SaveChangesAsync(cancellationToken);
            await _transactionRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Tokens allocated: {Tokens} (cumulative: {Total}) to user {UserId} by {AllocatedByUserId}", 
                request.Tokens, totalTokensForUser, request.UserId, allocatedByUserId);

            return new AllocateTokensResponse(
                Success: true,
                AllocationId: existingAllocation.Id,
                UserId: request.UserId,
                UserName: user.FullName,
                TokensAllocated: totalTokensForUser,
                SchoolTokensRemaining: account.TokensRemaining,
                ExpiresAt: existingAllocation.ExpiresAt,
                ErrorMessage: null
            );
        }
        catch (Exception ex)
        {
            var innerMessage = ex.InnerException?.Message ?? "No inner exception";
            _logger.LogError(ex, "Error allocating tokens to user {UserId}: {Message}\nInner: {InnerMessage}", 
                request.UserId, ex.Message, innerMessage);
            return new AllocateTokensResponse(
                Success: false,
                AllocationId: 0,
                UserId: request.UserId,
                UserName: "",
                TokensAllocated: 0,
                SchoolTokensRemaining: 0,
                ExpiresAt: null,
                ErrorMessage: $"{ex.Message} | Inner: {innerMessage}"
            );
        }
    }
}
