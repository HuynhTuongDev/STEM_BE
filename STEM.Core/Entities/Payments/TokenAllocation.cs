using STEM.Core.Entities.Users;

namespace STEM.Core.Entities.Payments;

public class TokenAllocation : BaseEntity
{
    public int AccountId { get; set; }
    public int UserId { get; set; }
    public int AllocatedTokens { get; set; }
    public int UsedTokens { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Notes { get; set; }
    public int AllocatedByUserId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? RevokedAt { get; set; }
    public string? RevocationReason { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public TokenAccount? Account { get; set; }
    public User? User { get; set; }
    public User? AllocatedByUser { get; set; }
}
