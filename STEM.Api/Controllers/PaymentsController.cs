using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STEM.Application.Dtos.Payments;
using STEM.Application.UseCases.Payments;
using STEM.Core.Entities.Payments;
using STEM.Core.Repository;
using System.Security.Claims;

namespace STEM.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly GetPackagesHandler _getPackagesHandler;
    private readonly CreatePaymentHandler _createPaymentHandler;
    private readonly PaymentCallbackHandler _paymentCallbackHandler;
    private readonly GetPaymentsHandler _getPaymentsHandler;
    private readonly GetTokenBalanceHandler _getTokenBalanceHandler;
    private readonly GetTokenTransactionsHandler _getTokenTransactionsHandler;
    private readonly UseTokenHandler _useTokenHandler;
    private readonly IUserRepository _userRepository;

    public PaymentsController(
        GetPackagesHandler getPackagesHandler,
        CreatePaymentHandler createPaymentHandler,
        PaymentCallbackHandler paymentCallbackHandler,
        GetPaymentsHandler getPaymentsHandler,
        GetTokenBalanceHandler getTokenBalanceHandler,
        GetTokenTransactionsHandler getTokenTransactionsHandler,
        UseTokenHandler useTokenHandler,
        IUserRepository userRepository)
    {
        _getPackagesHandler = getPackagesHandler;
        _createPaymentHandler = createPaymentHandler;
        _paymentCallbackHandler = paymentCallbackHandler;
        _getPaymentsHandler = getPaymentsHandler;
        _getTokenBalanceHandler = getTokenBalanceHandler;
        _getTokenTransactionsHandler = getTokenTransactionsHandler;
        _useTokenHandler = useTokenHandler;
        _userRepository = userRepository;
    }

    private int GetCurrentUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    /// <summary>
    /// Get all available payment packages
    /// </summary>
    [HttpGet("packages")]
    public async Task<IActionResult> GetPackages(CancellationToken cancellationToken = default)
    {
        try
        {
            var packages = await _getPackagesHandler.Handle(cancellationToken);
            return Ok(new { success = true, data = packages });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Create a new payment (School Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> CreatePayment(
        [FromBody] CreatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            var payment = await _createPaymentHandler.Handle(request, userId, cancellationToken);
            return Ok(new { success = true, data = payment });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Payment callback from payment gateway
    /// </summary>
    [HttpPost("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> PaymentCallback(
        [FromBody] PaymentCallbackRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _paymentCallbackHandler.Handle(request, cancellationToken);
            return Ok(new { success = true });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get payment history for current school
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> GetPayments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user?.SchoolId == null)
                return Forbid();

            var payments = await _getPaymentsHandler.Handle(user.SchoolId.Value, page, pageSize, cancellationToken);
            return Ok(new { success = true, data = payments });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get token balance for current school
    /// </summary>
    [HttpGet("balance")]
    [Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> GetTokenBalance(CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user?.SchoolId == null)
                return Forbid();

            var balance = await _getTokenBalanceHandler.Handle(user.SchoolId.Value, user.School?.Name ?? "", cancellationToken);
            return Ok(new { success = true, data = balance });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get token transaction history
    /// </summary>
    [HttpGet("transactions")]
    [Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> GetTokenTransactions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user?.SchoolId == null)
                return Forbid();

            var transactions = await _getTokenTransactionsHandler.Handle(user.SchoolId.Value, page, pageSize, cancellationToken);
            return Ok(new { success = true, data = transactions });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Use tokens (deduct from balance)
    /// </summary>
    [HttpPost("use")]
    [Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> UseTokens(
        [FromBody] UseTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user?.SchoolId == null)
                return Forbid();

            await _useTokenHandler.Handle(user.SchoolId.Value, request.Amount, request.Description, cancellationToken);
            return Ok(new { success = true });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}

public class UseTokenRequest
{
    public int Amount { get; set; }
    public string Description { get; set; } = "";
}
