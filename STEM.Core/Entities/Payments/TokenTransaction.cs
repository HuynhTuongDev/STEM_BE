namespace STEM.Core.Entities.Payments;

public enum TransactionType
{
    Purchase,
    Usage,
    Distribution,
    Revocation,
    Refund,
    Expiration,
    Adjustment
}

public class TokenTransaction : BaseEntity
{
    public int? PaymentId { get; set; }
    public int AccountId { get; set; }
    public TransactionType Type { get; set; }
    public int Quantity { get; set; }
    public int BalanceAfter { get; set; }
    public string? Description { get; set; }
    public string? ReferenceId { get; set; }
    public string? Metadata { get; set; }

    public Payment? Payment { get; set; }
    public TokenAccount? Account { get; set; }
}
