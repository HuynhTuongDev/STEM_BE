using STEM.Application.Interfaces;
using STEM.Core.Interfaces;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Simulation;

/// <summary>
/// Handler lấy thông tin AI quota của user hiện tại.
/// Trả về tổng quan quota: đã dùng hôm nay, tổng đã dùng, quota còn lại.
/// </summary>
public class GetUserAiQuotaHandler
{
    private readonly IAiQuotaUsageStore _quotaStore;
    private readonly ITokenAllocationRepository _allocationRepository;
    private readonly ITokenAccountRepository _accountRepository;
    private readonly IUserRepository _userRepository;

    public GetUserAiQuotaHandler(
        IAiQuotaUsageStore quotaStore,
        ITokenAllocationRepository allocationRepository,
        ITokenAccountRepository accountRepository,
        IUserRepository userRepository)
    {
        _quotaStore = quotaStore;
        _allocationRepository = allocationRepository;
        _accountRepository = accountRepository;
        _userRepository = userRepository;
    }

    public async Task<UserAiQuotaResponse> Handle(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return new UserAiQuotaResponse
            {
                Success = false,
                UserId = userId,
                TotalAllocated = 0,
                TotalUsed = 0,
                TodayUsed = 0,
                Remaining = 0,
                ExpiresAt = null,
                ErrorMessage = "User not found"
            };
        }

        var account = await _accountRepository.GetBySchoolIdAsync(user.SchoolId ?? 0, cancellationToken);
        var allocation = account != null 
            ? await _allocationRepository.GetActiveAllocationAsync(account.Id, userId, cancellationToken)
            : null;

        int totalAllocated;
        DateTime? expiresAt;

        if (allocation != null)
        {
            totalAllocated = allocation.AllocatedTokens;
            expiresAt = account?.ExpiresAt;
        }
        else if (account != null)
        {
            totalAllocated = account.TokensRemaining;
            expiresAt = account.ExpiresAt;
        }
        else
        {
            totalAllocated = 0;
            expiresAt = null;
        }

        var todayUsed = await _quotaStore.GetTodayUsedTokensAsync(userId, cancellationToken);
        var totalUsed = await _quotaStore.GetTotalUsedByUserAsync(userId, cancellationToken);
        
        int remaining;
        if (allocation != null)
        {
            remaining = Math.Max(0, allocation.AllocatedTokens - allocation.UsedTokens);
        }
        else if (account != null)
        {
            remaining = account.TokensRemaining;
        }
        else
        {
            remaining = 0;
        }

        // Check if expired - reset to 0 if past expiry date
        if (expiresAt.HasValue && expiresAt.Value < DateTime.UtcNow)
        {
            remaining = 0;
        }

        return new UserAiQuotaResponse
        {
            Success = true,
            UserId = userId,
            UserName = user.FullName,
            TotalAllocated = totalAllocated,
            TotalUsed = totalUsed,
            TodayUsed = todayUsed,
            Remaining = remaining,
            ExpiresAt = expiresAt,
            HasIndividualAllocation = allocation != null,
            ErrorMessage = null
        };
    }
}

public class UserAiQuotaResponse
{
    public bool Success { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public int TotalAllocated { get; set; }
    public int TotalUsed { get; set; }
    public int TodayUsed { get; set; }
    public int Remaining { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool HasIndividualAllocation { get; set; }
    public string? ErrorMessage { get; set; }
}
