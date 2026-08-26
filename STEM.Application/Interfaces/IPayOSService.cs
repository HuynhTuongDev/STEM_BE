namespace STEM.Application.Interfaces;

public interface IPayOSService
{
    Task<PayOSCreatePaymentResult> CreatePaymentLinkAsync(PayOSPaymentRequest request, CancellationToken cancellationToken = default);
    Task<PayOSPaymentResult?> GetPaymentStatusAsync(string paymentLinkId, CancellationToken cancellationToken = default);
    Task<bool> CancelPaymentLinkAsync(string paymentLinkId, string cancellationReason = "", CancellationToken cancellationToken = default);
    Task<PayOSWebhookResult> ProcessWebhookAsync(PayOSWebhookData webhookData, CancellationToken cancellationToken = default);
}

public class PayOSPaymentRequest
{
    public long OrderCode { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
    public int ExpiresAt { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public class PayOSCreatePaymentResult
{
    public bool Success { get; set; }
    public string? CheckoutUrl { get; set; }
    public string? PaymentLinkId { get; set; }
    public string? TransactionId { get; set; }
    public string? ErrorMessage { get; set; }
}

public class PayOSPaymentResult
{
    public string Status { get; set; } = string.Empty;
    public string? GatewayTransactionId { get; set; }
    public decimal Amount { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public class PayOSWebhookData
{
    public string Code { get; set; } = string.Empty;
    public string Desc { get; set; } = string.Empty;
    public string? Success { get; set; }
    public string? PaymentLinkId { get; set; }
    public string? OrderCode { get; set; }
    public long? Amount { get; set; }
    public string? TransactionId { get; set; }
    public string? TransactionDateTime { get; set; }
    public string? Signature { get; set; }
    public string? AccountNumber { get; set; }
    public string? SubAccount { get; set; }
    public string? Currency { get; set; }
}

public class PayOSWebhookResult
{
    public bool IsValid { get; set; }
    public string? OrderCode { get; set; }
    public string? PaymentLinkId { get; set; }
    public string? Status { get; set; }
    public string? ErrorMessage { get; set; }
}
