using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STEM.Application.Dtos.LoginHistory;
using STEM.Application.UseCases.LoginHistory;

namespace STEM.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LoginHistoryController : ControllerBase
{
    private readonly GetLoginHistoriesHandler _getLoginHistoriesHandler;

    public LoginHistoryController(GetLoginHistoriesHandler getLoginHistoriesHandler)
    {
        _getLoginHistoriesHandler = getLoginHistoriesHandler;
    }

    /// <summary>
    /// Lịch sử đăng nhập của user đang đăng nhập.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyLoginHistories(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new GetLoginHistoriesRequest
            {
                UserId = GetCurrentUserId(),
                PageNumber = pageNumber,
                PageSize = pageSize
            };
            var histories = await _getLoginHistoriesHandler.Handle(request, cancellationToken);
            return Ok(new { success = true, total = histories.Count, data = histories });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// School Admin xem lịch sử đăng nhập theo UserId.
    /// </summary>
    [HttpPost("get-histories")]
    [Authorize(Policy = "SchoolAdminOnly")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetLoginHistories(
        [FromBody] GetLoginHistoriesRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            return BadRequest(new { success = false, message = "Request body is required." });

        if (request.UserId <= 0)
            return BadRequest(new { success = false, message = "UserId is required." });

        try
        {
            var histories = await _getLoginHistoriesHandler.Handle(request, cancellationToken);
            return Ok(new { success = true, total = histories.Count, data = histories });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
            return userId;

        throw new UnauthorizedAccessException("User is not authenticated properly.");
    }
}
