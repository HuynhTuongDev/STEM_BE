namespace STEM.Core.Entities.Simulations;

/// <summary>
/// Theo dõi tổng token AI (Anthropic) mỗi user dùng trong 1 ngày (UTC) — chỉ cộng dồn
/// cho các lần thực sự gọi AI provider; Apply/Reject đề xuất là thao tác local, KHÔNG ghi vào đây.
/// </summary>
public class AiQuotaUsage
{
    public Guid Id { get; set; }
    public int UserId { get; set; }

    /// <summary>Ngày UTC (giờ đã cắt về 00:00:00) — 1 dòng/user/ngày.</summary>
    public DateTime UsageDate { get; set; }

    public int TotalTokens { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
