using STEM.Core.Entities.Schools;

namespace STEM.Core.Entities.Payments;

public class TokenAccount : BaseEntity
{
    public int SchoolId { get; set; }
    public int TotalTokensPurchased { get; set; }
    public int TokensRemaining { get; set; }
    public int TokensUsed { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastPurchaseAt { get; set; }

    public School? School { get; set; }
    public ICollection<TokenTransaction> Transactions { get; set; } = new List<TokenTransaction>();
    public ICollection<TokenAllocation> Allocations { get; set; } = new List<TokenAllocation>();
}
