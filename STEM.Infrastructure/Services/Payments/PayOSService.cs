using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using STEM.Application.Interfaces;
using Net.payOS;
using Net.payOS.Types;

namespace STEM.Infrastructure.Services.Payments;

public class PayOSService : IPayOSService
{
    private readonly ILogger<PayOSService> _logger;
    private readonly string _clientId;
    private readonly string _apiKey;
    private readonly string _checksumKey;
    private readonly string _frontendBaseUrl;

    public PayOSService(
        ILogger<PayOSService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _clientId = configuration["PayOS:ClientId"] ?? throw new ArgumentNullException(nameof(_clientId));
        _apiKey = configuration["PayOS:ApiKey"] ?? throw new ArgumentNullException(nameof(_apiKey));
        _checksumKey = configuration["PayOS:ChecksumKey"] ?? throw new ArgumentNullException(nameof(_checksumKey));
        _frontendBaseUrl = configuration["AppSettings:FrontendBaseUrl"] ?? "http://localhost:5173";
    }

    public async Task<PayOSCreatePaymentResult> CreatePaymentLinkAsync(
        PayOSPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payOS = new PayOS(_clientId, _apiKey, _checksumKey);
            var expiresAt = DateTimeOffset.UtcNow.AddHours(24).ToUnixTimeSeconds();

            var items = new List<ItemData>
            {
                new ItemData(request.Description, 1, (int)request.Amount)
            };

            var paymentData = new PaymentData(
                orderCode: request.OrderCode,
                amount: (int)request.Amount,
                description: request.Description,
                items: items,
                returnUrl: request.ReturnUrl,
                cancelUrl: request.CancelUrl,
                expiredAt: expiresAt
            );

            var createdPayment = await payOS.createPaymentLink(paymentData);

            _logger.LogInformation("PayOS payment link created: {PaymentLinkId}, OrderCode: {OrderCode}", 
                createdPayment.paymentLinkId, createdPayment.orderCode);

            return new PayOSCreatePaymentResult
            {
                Success = true,
                CheckoutUrl = createdPayment.checkoutUrl,
                PaymentLinkId = createdPayment.paymentLinkId,
                TransactionId = createdPayment.orderCode.ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create PayOS payment link for OrderCode: {OrderCode}", request.OrderCode);
            
            return new PayOSCreatePaymentResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<PayOSPaymentResult?> GetPaymentStatusAsync(
        string paymentLinkId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payOS = new PayOS(_clientId, _apiKey, _checksumKey);
            var paymentInfo = await payOS.getPaymentLinkInformation(long.Parse(paymentLinkId));

            return new PayOSPaymentResult
            {
                Status = paymentInfo.status.ToString().ToUpper(),
                GatewayTransactionId = paymentInfo.orderCode.ToString(),
                Amount = paymentInfo.amount,
                PaidAt = null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get PayOS payment status for PaymentLinkId: {PaymentLinkId}", paymentLinkId);
            return null;
        }
    }

    public async Task<bool> CancelPaymentLinkAsync(
        string paymentLinkId,
        string cancellationReason = "",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payOS = new PayOS(_clientId, _apiKey, _checksumKey);
            await payOS.cancelPaymentLink(long.Parse(paymentLinkId), cancellationReason);
            _logger.LogInformation("PayOS payment link cancelled: {PaymentLinkId}", paymentLinkId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel PayOS payment link: {PaymentLinkId}", paymentLinkId);
            return false;
        }
    }

    public Task<PayOSWebhookResult> ProcessWebhookAsync(
        PayOSWebhookData webhookData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (webhookData.Code != "00")
            {
                return Task.FromResult(new PayOSWebhookResult
                {
                    IsValid = true,
                    Status = MapPaymentStatus(webhookData.Code),
                    OrderCode = webhookData.OrderCode,
                    PaymentLinkId = webhookData.PaymentLinkId
                });
            }

            return Task.FromResult(new PayOSWebhookResult
            {
                IsValid = true,
                OrderCode = webhookData.OrderCode,
                PaymentLinkId = webhookData.PaymentLinkId,
                Status = webhookData.Success?.ToLower() == "true" ? "COMPLETED" : "PENDING"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process PayOS webhook");
            return Task.FromResult(new PayOSWebhookResult
            {
                IsValid = false,
                ErrorMessage = ex.Message
            });
        }
    }

    private static string MapPaymentStatus(string status)
    {
        return status?.ToUpper() switch
        {
            "00" or "PAID" or "COMPLETED" => "COMPLETED",
            "01" or "PENDING" => "PENDING",
            "02" or "CANCELLED" => "CANCELLED",
            "03" or "EXPIRED" => "EXPIRED",
            _ => status ?? "UNKNOWN"
        };
    }
}
