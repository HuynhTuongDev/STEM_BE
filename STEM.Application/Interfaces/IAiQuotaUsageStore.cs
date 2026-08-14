namespace STEM.Application.Interfaces;

/// <summary>Theo dõi tổng token AI (Anthropic) mỗi user dùng trong ngày UTC hiện tại.</summary>
public interface IAiQuotaUsageStore
{
    Task<int> GetTodayUsedTokensAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Cộng dồn token vào ngày UTC hiện tại, trả về tổng token đã dùng trong ngày SAU khi cộng.</summary>
    Task<int> AddTodayUsageAsync(int userId, int tokens, CancellationToken cancellationToken = default);
}
