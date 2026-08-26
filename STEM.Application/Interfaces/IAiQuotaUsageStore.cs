namespace STEM.Application.Interfaces;

/// <summary>
/// Theo dõi tổng token AI (Anthropic) mỗi user dùng trong ngày UTC hiện tại và tổng quan.
/// </summary>
public interface IAiQuotaUsageStore
{
    /// <summary>Lấy số token đã dùng trong ngày UTC hiện tại.</summary>
    Task<int> GetTodayUsedTokensAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Cộng dồn token vào ngày UTC hiện tại, trả về tổng token đã dùng trong ngày SAU khi cộng.</summary>
    Task<int> AddTodayUsageAsync(int userId, int tokens, CancellationToken cancellationToken = default);

    /// <summary>Lấy tổng số token đã dùng từ đầu (tất cả các ngày).</summary>
    Task<int> GetTotalUsedByUserAsync(int userId, CancellationToken cancellationToken = default);
}
