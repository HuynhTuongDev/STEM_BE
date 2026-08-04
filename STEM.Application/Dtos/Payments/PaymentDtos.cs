using STEM.Core.Entities.Payments;

namespace STEM.Application.Dtos.Payments;

public class CreatePaymentRequest
{
    public int PackageId { get; set; }
    public PaymentMethod Method { get; set; }
}

public class PaymentCallbackRequest
{
    public string TransactionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? GatewayTransactionId { get; set; }
    public string? Signature { get; set; }
}

public class PaymentResponse
{
    public int Id { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public int PackageId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public int DurationMonths { get; set; }
    public int TokenAmount { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? PaymentGateway { get; set; }
    public string? GatewayTransactionId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PaymentPackageResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DurationMonths { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public int TokenAmount { get; set; }
    public bool IsActive { get; set; }
    public bool IsFeatured { get; set; }
    public string? Features { get; set; }
}

public class TokenBalanceResponse
{
    public int SchoolId { get; set; }
    public string SchoolName { get; set; } = string.Empty;
    public int TotalTokensPurchased { get; set; }
    public int TokensRemaining { get; set; }
    public int TokensUsed { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastPurchaseAt { get; set; }
}

public class TokenTransactionResponse
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int PaymentId { get; set; }
    public string Type { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int BalanceAfter { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PaymentListResponse
{
    public List<PaymentResponse> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
