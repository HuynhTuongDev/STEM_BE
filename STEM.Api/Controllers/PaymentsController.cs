using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using STEM.Application.DTOs.Payments;
using STEM.Application.UseCases.Payments;
using STEM.Core.Entities.Payments;
using STEM.Core.Interfaces;

namespace STEM.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly ILogger<PaymentsController> _logger;
    private readonly IPaymentRepository _paymentRepository;
    private readonly GetPackagesHandler _getPackagesHandler;
    private readonly CreatePaymentHandler _createPaymentHandler;
    private readonly GetBalanceHandler _getBalanceHandler;
    private readonly GetPaymentsHandler _getPaymentsHandler;
    private readonly GetAllocationsHandler _getAllocationsHandler;
    private readonly GetTransactionsHandler _getTransactionsHandler;
    private readonly AllocateTokensHandler _allocateTokensHandler;
    private readonly RevokeAllocationHandler _revokeAllocationHandler;
    private readonly PaymentWebhookHandler _paymentWebhookHandler;
    private readonly GetUsersWithTokensHandler _getUsersWithTokensHandler;
    private readonly CreatePackageHandler _createPackageHandler;
    private readonly UpdatePackageHandler _updatePackageHandler;
    private readonly DeletePackageHandler _deletePackageHandler;
    private readonly BulkAllocateTokensByRoleHandler _bulkAllocateHandler;

    public PaymentsController(
        ILogger<PaymentsController> logger,
        IPaymentRepository paymentRepository,
        GetPackagesHandler getPackagesHandler,
        CreatePaymentHandler createPaymentHandler,
        GetBalanceHandler getBalanceHandler,
        GetPaymentsHandler getPaymentsHandler,
        GetAllocationsHandler getAllocationsHandler,
        GetTransactionsHandler getTransactionsHandler,
        AllocateTokensHandler allocateTokensHandler,
        RevokeAllocationHandler revokeAllocationHandler,
        PaymentWebhookHandler paymentWebhookHandler,
        GetUsersWithTokensHandler getUsersWithTokensHandler,
        CreatePackageHandler createPackageHandler,
        UpdatePackageHandler updatePackageHandler,
        DeletePackageHandler deletePackageHandler,
        BulkAllocateTokensByRoleHandler bulkAllocateHandler)
    {
        _logger = logger;
        _paymentRepository = paymentRepository;
        _getPackagesHandler = getPackagesHandler;
        _createPaymentHandler = createPaymentHandler;
        _getBalanceHandler = getBalanceHandler;
        _getPaymentsHandler = getPaymentsHandler;
        _getAllocationsHandler = getAllocationsHandler;
        _getTransactionsHandler = getTransactionsHandler;
        _allocateTokensHandler = allocateTokensHandler;
        _revokeAllocationHandler = revokeAllocationHandler;
        _paymentWebhookHandler = paymentWebhookHandler;
        _getUsersWithTokensHandler = getUsersWithTokensHandler;
        _createPackageHandler = createPackageHandler;
        _updatePackageHandler = updatePackageHandler;
        _deletePackageHandler = deletePackageHandler;
        _bulkAllocateHandler = bulkAllocateHandler;
    }

    private int GetCurrentUserId() =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    private int GetCurrentSchoolId() =>
        int.Parse(User.FindFirst("SchoolId")?.Value ?? "0");

    private string GetCurrentSchoolName() =>
        User.FindFirst("SchoolName")?.Value ?? "";

    private int GetCurrentRoleId() =>
        int.Parse(User.FindFirst("RoleId")?.Value ?? "0");

    private bool IsMasterAdmin() => GetCurrentRoleId() == 1;
    private bool IsSchoolAdmin() => GetCurrentRoleId() == 2;

    [HttpGet("packages")]
    public async Task<IActionResult> GetPackages(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _getPackagesHandler.Handle(cancellationToken: cancellationToken);
            return Ok(new { success = true, data = response.Packages });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get packages.", error = ex.Message });
        }
    }

    [HttpGet("admin/packages")]
    [Authorize(Roles = "Master Administrator")]
    public async Task<IActionResult> GetAllPackages(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _getPackagesHandler.Handle(includeInactive, cancellationToken);
            return Ok(new { success = true, data = response.Packages });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get packages.", error = ex.Message });
        }
    }

    [HttpPost("admin/packages")]
    [Authorize(Roles = "Master Administrator")]
    public async Task<IActionResult> CreatePackage(
        [FromBody] CreatePackageRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _createPackageHandler.Handle(request, cancellationToken);
            if (result == null)
            {
                return BadRequest(new { success = false, message = "Failed to create package." });
            }
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to create package.", error = ex.Message });
        }
    }

    [HttpPut("admin/packages/{id:int}")]
    [Authorize(Roles = "Master Administrator")]
    public async Task<IActionResult> UpdatePackage(
        int id,
        [FromBody] UpdatePackageRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _updatePackageHandler.Handle(id, request, cancellationToken);
            if (result == null)
            {
                return NotFound(new { success = false, message = "Package not found." });
            }
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to update package.", error = ex.Message });
        }
    }

    [HttpDelete("admin/packages/{id:int}")]
    [Authorize(Roles = "Master Administrator")]
    public async Task<IActionResult> DeletePackage(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _deletePackageHandler.Handle(id, cancellationToken);
            if (!result)
            {
                return NotFound(new { success = false, message = "Package not found." });
            }
            return Ok(new { success = true, message = "Package deleted successfully." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to delete package.", error = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> CreatePayment(
        [FromBody] CreatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!IsSchoolAdmin())
            {
                return Forbid();
            }

            var schoolId = GetCurrentSchoolId();
            var schoolName = GetCurrentSchoolName();

            var result = await _createPaymentHandler.Handle(request, schoolId, schoolName, cancellationToken);
            return Ok(new { success = result.Success, data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to create payment.", error = ex.Message });
        }
    }

    [HttpGet("balance")]
    [Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> GetBalance(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!IsSchoolAdmin())
            {
                return Forbid();
            }

            var schoolId = GetCurrentSchoolId();
            var schoolName = GetCurrentSchoolName();

            var result = await _getBalanceHandler.Handle(schoolId, schoolName, cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get balance.", error = ex.Message });
        }
    }

    [HttpGet]
    [Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> GetPayments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!IsSchoolAdmin())
            {
                return Forbid();
            }

            var schoolId = GetCurrentSchoolId();
            var result = await _getPaymentsHandler.Handle(schoolId, page, pageSize, cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get payments.", error = ex.Message });
        }
    }

    [HttpGet("transactions")]
    [Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!IsSchoolAdmin())
            {
                return Forbid();
            }

            var schoolId = GetCurrentSchoolId();
            var result = await _getTransactionsHandler.Handle(schoolId, page, pageSize, cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get transactions.", error = ex.Message });
        }
    }

    [HttpGet("allocations")]
    [Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> GetAllocations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!IsSchoolAdmin())
            {
                return Forbid();
            }

            var schoolId = GetCurrentSchoolId();
            var result = await _getAllocationsHandler.Handle(schoolId, page, pageSize, cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get allocations.", error = ex.Message });
        }
    }

    [HttpPost("allocate")]
    [Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> AllocateTokens(
        [FromBody] AllocateTokensRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!IsSchoolAdmin())
            {
                return Forbid();
            }

            var schoolId = GetCurrentSchoolId();
            var userId = GetCurrentUserId();

            var result = await _allocateTokensHandler.Handle(request, schoolId, userId, cancellationToken);
            return Ok(new { success = result.Success, data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to allocate tokens.", error = ex.Message });
        }
    }

    [HttpDelete("allocations/{id:int}")]
    [Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> RevokeAllocation(
        int id,
        [FromQuery] string? reason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!IsSchoolAdmin())
            {
                return Forbid();
            }

            var result = await _revokeAllocationHandler.Handle(id, reason, cancellationToken);
            return Ok(new { success = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to revoke allocation.", error = ex.Message });
        }
    }

    [HttpPost("bulk-allocate")]
    [Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> BulkAllocateTokens(
        [FromBody] BulkAllocationByRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!IsSchoolAdmin())
            {
                return Forbid();
            }

            var schoolId = GetCurrentSchoolId();
            var userId = GetCurrentUserId();

            var result = await _bulkAllocateHandler.Handle(request, schoolId, userId, cancellationToken);
            return Ok(new { success = result.Success, data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to bulk allocate tokens.", error = ex.Message });
        }
    }

    [HttpGet("users-with-tokens")]
    [Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> GetUsersWithTokens(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!IsSchoolAdmin())
            {
                return Forbid();
            }

            var schoolId = GetCurrentSchoolId();
            var result = await _getUsersWithTokensHandler.Handle(schoolId, cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get users.", error = ex.Message });
        }
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> PaymentWebhook(
        [FromBody] PayOSWebhookRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("PayOS Webhook received: Code={Code}, OrderCode={OrderCode}, PaymentLinkId={PaymentLinkId}", 
                request.Code, request.OrderCode, request.PaymentLinkId);

            string status;
            if (request.Code == "00")
            {
                status = "COMPLETED";
            }
            else if (request.Code == "01" || request.Code == "24")
            {
                status = "CANCELLED";
            }
            else if (request.Code == "02")
            {
                status = "EXPIRED";
            }
            else
            {
                status = "FAILED";
            }

            var result = await _paymentWebhookHandler.Handle(
                transactionId: "",
                status: status,
                gatewayTransactionId: request.TransactionId?.ToString(),
                orderCode: request.OrderCode,
                paymentLinkId: request.PaymentLinkId,
                cancellationToken: cancellationToken);

            return Ok(new { success = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Webhook processing failed.", error = ex.Message });
        }
    }

    [HttpPost("callback")]
    public async Task<IActionResult> PaymentCallback(
        [FromBody] PaymentCallbackRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _paymentWebhookHandler.Handle(
                transactionId: request.TransactionId,
                status: request.Status,
                gatewayTransactionId: request.GatewayTransactionId,
                orderCode: null,
                paymentLinkId: null,
                cancellationToken: cancellationToken);

            return Ok(new { success = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Callback processing failed.", error = ex.Message });
        }
    }

    [HttpPost("manual-complete/{paymentId}")]
    [AllowAnonymous]
    public async Task<IActionResult> ManualCompletePayment(
        int paymentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Manual complete payment requested for PaymentId: {PaymentId}", paymentId);
            
            var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken);
            if (payment == null)
            {
                return NotFound(new { message = "Payment not found", paymentId });
            }

            var result = await _paymentWebhookHandler.Handle(
                transactionId: payment.TransactionId,
                status: "COMPLETED",
                gatewayTransactionId: "MANUAL-" + DateTime.UtcNow.Ticks,
                orderCode: payment.OrderCode,
                paymentLinkId: payment.PaymentLinkId,
                cancellationToken: cancellationToken);

            return Ok(new { success = result, paymentId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manual complete payment failed for PaymentId: {PaymentId}", paymentId);
            return StatusCode(500, new { success = false, message = "Manual complete failed.", error = ex.Message });
        }
    }

    [HttpGet("debug/{transactionId}")]
    [AllowAnonymous]
    public async Task<IActionResult> DebugPayment(
        string transactionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payment = await _paymentRepository.GetByTransactionIdAsync(transactionId, cancellationToken);
            if (payment == null)
            {
                return NotFound(new { message = "Payment not found", transactionId });
            }

            return Ok(new 
            { 
                paymentId = payment.Id,
                transactionId = payment.TransactionId,
                orderCode = payment.OrderCode,
                status = payment.Status.ToString(),
                tokenAmount = payment.TokenAmount,
                schoolId = payment.SchoolId
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    [HttpPost("complete-by-order/{orderCode}")]
    [AllowAnonymous]
    public async Task<IActionResult> CompleteByOrderCode(
        long orderCode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Complete by OrderCode requested: {OrderCode}", orderCode);
            
            var payment = await _paymentRepository.GetByOrderCodeAsync(orderCode, cancellationToken);
            if (payment == null)
            {
                _logger.LogWarning("Payment not found for OrderCode: {OrderCode}", orderCode);
                return NotFound(new { message = "Payment not found", orderCode });
            }

            return await CompletePaymentInternal(payment.Id, orderCode, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Complete by OrderCode failed: {OrderCode}", orderCode);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    [HttpPost("complete/{paymentId}")]
    [AllowAnonymous]
    public async Task<IActionResult> CompletePayment(
        int paymentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken);
            if (payment == null)
            {
                return NotFound(new { message = "Payment not found", paymentId });
            }

            return await CompletePaymentInternal(paymentId, payment.OrderCode, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Complete payment failed: {PaymentId}", paymentId);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    private async Task<IActionResult> CompletePaymentInternal(int paymentId, long? orderCode, CancellationToken cancellationToken)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken);
            if (payment == null)
            {
                return NotFound(new { message = "Payment not found", paymentId });
            }

            _logger.LogInformation("Completing payment: Id={PaymentId}, TransactionId={TransactionId}, Status={Status}", 
                payment.Id, payment.TransactionId, payment.Status);

            if (payment.Status == PaymentStatus.Completed)
            {
                return Ok(new { success = true, message = "Payment already completed", paymentId });
            }

            var result = await _paymentWebhookHandler.Handle(
                transactionId: payment.TransactionId,
                status: "COMPLETED",
                gatewayTransactionId: "MANUAL-" + (orderCode?.ToString() ?? payment.Id.ToString()),
                orderCode: orderCode,
                paymentLinkId: payment.PaymentLinkId,
                cancellationToken: cancellationToken);

            if (result)
            {
                return Ok(new { success = true, message = "Payment completed successfully", paymentId, tokensAdded = payment.TokenAmount });
            }
            else
            {
                return BadRequest(new { success = false, message = "Failed to complete payment" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CompletePaymentInternal failed: {PaymentId}", paymentId);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    [HttpPost("update-order/{paymentId}")]
    [AllowAnonymous]
    public async Task<IActionResult> UpdateOrderCode(
        int paymentId,
        [FromBody] UpdateOrderCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken);
            if (payment == null)
            {
                return NotFound(new { message = "Payment not found", paymentId });
            }

            payment.OrderCode = request.OrderCode;
            _paymentRepository.Update(payment);
            await _paymentRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated OrderCode for Payment {PaymentId}: {OrderCode}", paymentId, request.OrderCode);

            return Ok(new { success = true, paymentId, orderCode = request.OrderCode });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update OrderCode failed for PaymentId: {PaymentId}", paymentId);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}

public class UpdateOrderCodeRequest
{
    public long OrderCode { get; set; }
}

public class PayOSWebhookRequest
{
    public string Code { get; set; } = string.Empty;
    public string? Desc { get; set; }
    public string? Success { get; set; }
    public string? PaymentLinkId { get; set; }
    public long? OrderCode { get; set; }
    public long? Amount { get; set; }
    public long? TransactionId { get; set; }
    public string? TransactionDateTime { get; set; }
    public string? Signature { get; set; }
    public string? AccountNumber { get; set; }
    public string? SubAccount { get; set; }
    public string? Currency { get; set; }
}

public class PaymentCallbackRequest
{
    public string TransactionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? GatewayTransactionId { get; set; }
}
