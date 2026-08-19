using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using STEM.Application.DTOs.Payments;
using STEM.Application.Interfaces;
using STEM.Core.Entities.Payments;
using STEM.Core.Interfaces;

namespace STEM.Application.UseCases.Payments;

public class CreatePaymentHandler
{
    private readonly IPaymentPackageRepository _packageRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ITokenAccountRepository _accountRepository;
    private readonly IPayOSService _payOSService;
    private readonly ILogger<CreatePaymentHandler> _logger;
    private readonly string _frontendBaseUrl;

    public CreatePaymentHandler(
        IPaymentPackageRepository packageRepository,
        IPaymentRepository paymentRepository,
        ITokenAccountRepository accountRepository,
        IPayOSService payOSService,
        ILogger<CreatePaymentHandler> logger,
        IConfiguration configuration)
    {
        _packageRepository = packageRepository;
        _paymentRepository = paymentRepository;
        _accountRepository = accountRepository;
        _payOSService = payOSService;
        _logger = logger;
        _frontendBaseUrl = configuration["AppSettings:FrontendBaseUrl"] ?? "http://localhost:5173";
    }

    public async Task<CreatePaymentResponse> Handle(
        CreatePaymentRequest request,
        int schoolId,
        string schoolName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var package = await _packageRepository.GetByIdAsync(request.PackageId, cancellationToken);
            if (package == null || !package.IsActive)
            {
                return new CreatePaymentResponse(
                    Success: false,
                    PaymentId: 0,
                    TransactionId: "",
                    CheckoutUrl: null,
                    PaymentLinkId: null,
                    Amount: 0,
                    Currency: "VND",
                    Status: "FAILED",
                    ErrorMessage: "Package not found or inactive"
                );
            }

            var transactionId = $"STEM-{DateTime.UtcNow:yyyyMMddHHmmss}-{schoolId}-{request.PackageId}";
            
            // PayOS OrderCode must be numeric and fit in long
            // Use Unix timestamp (seconds) + schoolId + packageId
            var unixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var orderCode = long.Parse($"{unixSeconds}{schoolId:D4}{request.PackageId:D2}");
            
            var payment = new Payment
            {
                TransactionId = transactionId,
                OrderCode = orderCode,
                PackageId = request.PackageId,
                SchoolId = schoolId,
                TokenAmount = package.TokenAmount,
                Amount = package.Price,
                Currency = package.Currency,
                Status = PaymentStatus.Pending,
                Method = PaymentMethod.PayOS,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            await _paymentRepository.AddAsync(payment, cancellationToken);
            await _paymentRepository.SaveChangesAsync(cancellationToken); // Save immediately to get generated Id

            var returnUrl = $"{_frontendBaseUrl}/dashboard/payments?success=true&transactionId={transactionId}";
            var cancelUrl = $"{_frontendBaseUrl}/dashboard/payments?cancelled=true&transactionId={transactionId}";

            var payOSRequest = new PayOSPaymentRequest
            {
                OrderCode = orderCode, // Use numeric OrderCode
                Amount = package.Price,
                Description = $"STEM: {package.Name}",
                ReturnUrl = returnUrl,
                CancelUrl = cancelUrl,
                Metadata = new Dictionary<string, string>
                {
                    { "paymentId", payment.Id.ToString() },
                    { "schoolId", schoolId.ToString() },
                    { "packageId", package.Id.ToString() },
                    { "transactionId", transactionId }
                }
            };

            var payOSResult = await _payOSService.CreatePaymentLinkAsync(payOSRequest, cancellationToken);

            if (!payOSResult.Success)
            {
                payment.Status = PaymentStatus.Failed;
                _paymentRepository.Update(payment);
                await _paymentRepository.SaveChangesAsync(cancellationToken);

                return new CreatePaymentResponse(
                    Success: false,
                    PaymentId: payment.Id,
                    TransactionId: transactionId,
                    CheckoutUrl: null,
                    PaymentLinkId: null,
                    Amount: package.Price,
                    Currency: package.Currency,
                    Status: "FAILED",
                    ErrorMessage: payOSResult.ErrorMessage ?? "Failed to create payment link"
                );
            }

            payment.PaymentLinkId = payOSResult.PaymentLinkId;
            payment.CheckoutUrl = payOSResult.CheckoutUrl;
            _paymentRepository.Update(payment);
            await _paymentRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Payment created successfully: {TransactionId}, Package: {PackageName}", 
                transactionId, package.Name);

            return new CreatePaymentResponse(
                Success: true,
                PaymentId: payment.Id,
                TransactionId: transactionId,
                CheckoutUrl: payOSResult.CheckoutUrl,
                PaymentLinkId: payOSResult.PaymentLinkId,
                Amount: package.Price,
                Currency: package.Currency,
                Status: "PENDING",
                ErrorMessage: null
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment for school {SchoolId}, package {PackageId}", schoolId, request.PackageId);
            
            return new CreatePaymentResponse(
                Success: false,
                PaymentId: 0,
                TransactionId: "",
                CheckoutUrl: null,
                PaymentLinkId: null,
                Amount: 0,
                Currency: "VND",
                Status: "FAILED",
                ErrorMessage: ex.Message
            );
        }
    }
}
