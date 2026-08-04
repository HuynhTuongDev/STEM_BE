using STEM.Core.Entities.Common;
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

    // Navigation
    public School? School { get; set; }
}
