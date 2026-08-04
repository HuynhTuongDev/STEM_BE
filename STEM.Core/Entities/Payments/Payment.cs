using STEM.Core.Entities.Common;
using STEM.Core.Entities.Schools;
using STEM.Core.Entities.Users;

namespace STEM.Core.Entities.Payments;

public class Payment : BaseEntity
{
    public string TransactionId { get; set; } = Guid.NewGuid().ToString();
    public int BuyerId { get; set; } // School Admin user ID
    public int SellerId { get; set; } // Master Admin user ID
    public int PackageId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public PaymentMethod Method { get; set; } = PaymentMethod.Unknown;
    public DateTime? PaidAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? PaymentGateway { get; set; } // e.g., "PayOS", "Stripe", "Manual"
    public string? GatewayTransactionId { get; set; }
    public string? FailureReason { get; set; }
    public int TokenQuantity { get; set; } // Number of tokens purchased
    public int TokensRemaining { get; set; } // Tokens left after purchase
    public string? Metadata { get; set; } // JSON for extra data

    // Navigation
    public User? Buyer { get; set; }
    public User? Seller { get; set; }
    public PaymentPackage? Package { get; set; }
}

public enum PaymentStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Refunded = 4,
    Expired = 5
}

public enum PaymentMethod
{
    Unknown = 0,
    CreditCard = 1,
    BankTransfer = 2,
    PayOS = 3,
    Momo = 4,
    ZaloPay = 5,
    Manual = 6
}
