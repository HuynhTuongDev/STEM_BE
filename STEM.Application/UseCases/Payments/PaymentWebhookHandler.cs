using Microsoft.Extensions.Logging;
using STEM.Application.DTOs.Payments;
using STEM.Application.Interfaces;
using STEM.Core.Entities.Payments;
using STEM.Core.Interfaces;

namespace STEM.Application.UseCases.Payments;

public class PaymentWebhookHandler
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentPackageRepository _packageRepository;
    private readonly ITokenAccountRepository _accountRepository;
    private readonly ITokenTransactionRepository _transactionRepository;
    private readonly IPayOSService _payOSService;
    private readonly ILogger<PaymentWebhookHandler> _logger;

    public PaymentWebhookHandler(
        IPaymentRepository paymentRepository,
        IPaymentPackageRepository packageRepository,
        ITokenAccountRepository accountRepository,
        ITokenTransactionRepository transactionRepository,
        IPayOSService payOSService,
        ILogger<PaymentWebhookHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _packageRepository = packageRepository;
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _payOSService = payOSService;
        _logger = logger;
    }

    public async Task<bool> Handle(
        string transactionId,
        string status,
        string? gatewayTransactionId = null,
        long? orderCode = null,
        string? paymentLinkId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payment = await _paymentRepository.GetByOrderCodeAsync(orderCode ?? 0, cancellationToken)
                ?? await _paymentRepository.GetByPaymentLinkIdAsync(paymentLinkId ?? "", cancellationToken)
                ?? await _paymentRepository.GetByTransactionIdAsync(transactionId, cancellationToken);

            if (payment == null)
            {
                _logger.LogWarning("Payment not found for transaction: {TransactionId}, OrderCode: {OrderCode}, PaymentLinkId: {PaymentLinkId}", 
                    transactionId, orderCode, paymentLinkId);
                return false;
            }

            _logger.LogInformation("Found payment: {PaymentId}, TransactionId: {TransactionId}, OrderCode: {OrderCode}", 
                payment.Id, payment.TransactionId, payment.OrderCode);

            if (status.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase) ||
                status.Equals("PAID", StringComparison.OrdinalIgnoreCase) ||
                status.Equals("00", StringComparison.OrdinalIgnoreCase) ||
                status.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase))
            {
                // Check if already processed
                if (payment.Status == PaymentStatus.Completed)
                {
                    _logger.LogInformation("Payment already processed: {TransactionId}", transactionId);
                    return true;
                }

                var package = await _packageRepository.GetByIdAsync(payment.PackageId, cancellationToken);
                if (package == null)
                {
                    _logger.LogError("Package not found for payment: {PaymentId}", payment.Id);
                    return false;
                }

                payment.Status = PaymentStatus.Completed;
                payment.GatewayTransactionId = gatewayTransactionId;
                payment.PaidAt = DateTime.UtcNow;

                if (payment.SchoolId.HasValue)
                {
                    var account = await _accountRepository.GetOrCreateBySchoolIdAsync(payment.SchoolId.Value, cancellationToken);
                    account.TokensRemaining += payment.TokenAmount;
                    account.TotalTokensPurchased += payment.TokenAmount;
                    account.LastPurchaseAt = DateTime.UtcNow;
                    
                    // Calculate new expiry: extend from current expiry (if still valid) or from now
                    var currentExpiry = account.ExpiresAt;
                    var baseDate = (currentExpiry.HasValue && currentExpiry.Value > DateTime.UtcNow)
                        ? currentExpiry.Value
                        : DateTime.UtcNow;
                    
                    // Add DurationMonths from package
                    account.ExpiresAt = baseDate.AddMonths(package.DurationMonths);
                    
                    _accountRepository.Update(account);

                    var transaction = new TokenTransaction
                    {
                        PaymentId = payment.Id,
                        AccountId = account.Id,
                        Type = TransactionType.Purchase,
                        Quantity = payment.TokenAmount,
                        BalanceAfter = account.TokensRemaining,
                        Description = $"Purchased {payment.TokenAmount} tokens via PayOS - Package: {package.Name} ({package.DurationMonths} months)"
                    };
                    await _transactionRepository.AddAsync(transaction, cancellationToken);
                    await _transactionRepository.SaveChangesAsync(cancellationToken);
                }

                _paymentRepository.Update(payment);
                await _paymentRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Payment completed: {TransactionId}, Tokens added: {TokenAmount}", 
                    transactionId, payment.TokenAmount);

                return true;
            }
            else if (status.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase) || 
                     status.Equals("FAILED", StringComparison.OrdinalIgnoreCase) ||
                     status.Equals("EXPIRED", StringComparison.OrdinalIgnoreCase))
            {
                payment.Status = Enum.Parse<PaymentStatus>(status, true);
                payment.CanceledAt = DateTime.UtcNow;
                _paymentRepository.Update(payment);
                await _paymentRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Payment {Status}: {TransactionId}", status, transactionId);

                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook for transaction: {TransactionId}", transactionId);
            return false;
        }
    }
}
