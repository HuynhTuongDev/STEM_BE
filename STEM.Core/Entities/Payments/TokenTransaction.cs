using STEM.Core.Entities.Common;
using STEM.Core.Entities.Schools;

namespace STEM.Core.Entities.Payments;

public class TokenTransaction : BaseEntity
{
    public int SchoolId { get; set; }
    public int PaymentId { get; set; }
    public TokenTransactionType Type { get; set; }
    public int Quantity { get; set; }
    public int BalanceAfter { get; set; }
    public string? Description { get; set; }

    // Navigation
    public School? School { get; set; }
    public Payment? Payment { get; set; }
}

public enum TokenTransactionType
{
    Purchase = 1,
    Usage = 2,
    Refund = 3,
    Bonus = 4
}
