using Microsoft.Extensions.Logging;
using STEM.Application.DTOs.Payments;
using STEM.Core.Interfaces;

namespace STEM.Application.UseCases.Payments;

public class GetPaymentsHandler
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILogger<GetPaymentsHandler> _logger;

    public GetPaymentsHandler(
        IPaymentRepository paymentRepository,
        ILogger<GetPaymentsHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _logger = logger;
    }

    public async Task<PaymentListResponse> Handle(
        int schoolId,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payments = await _paymentRepository.GetBySchoolIdAsync(schoolId, page, pageSize, cancellationToken);
            var total = await _paymentRepository.GetCountBySchoolIdAsync(schoolId, cancellationToken);

            var paymentDtos = payments.Select(p => new PaymentDto(
                p.Id,
                p.TransactionId,
                p.PackageId,
                p.Package?.Name ?? "Unknown",
                p.Package?.DurationMonths ?? 0,
                p.TokenAmount,
                p.Amount,
                p.Currency,
                p.Status.ToString().ToUpper(),
                p.Method.ToString(),
                p.GatewayTransactionId,
                p.PaidAt,
                p.ExpiresAt,
                p.CreatedAt
            )).ToList();

            return new PaymentListResponse(paymentDtos, total, page, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payments for school {SchoolId}", schoolId);
            return new PaymentListResponse(new List<PaymentDto>(), 0, page, pageSize);
        }
    }
}
