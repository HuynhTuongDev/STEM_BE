using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STEM.Application.Dtos.Grading;
using STEM.Application.UseCases.Grading;

namespace STEM.Api.Controllers;

[ApiController]
[Route("api/resubmit-requests")]
[Authorize]
public class ResubmitRequestsController : ControllerBase
{
    private readonly CreateResubmitRequestHandler _createHandler;
    private readonly GetResubmitRequestsHandler _getHandler;
    private readonly ApproveResubmitRequestHandler _approveHandler;
    private readonly RejectResubmitRequestHandler _rejectHandler;

    public ResubmitRequestsController(
        CreateResubmitRequestHandler createHandler,
        GetResubmitRequestsHandler getHandler,
        ApproveResubmitRequestHandler approveHandler,
        RejectResubmitRequestHandler rejectHandler)
    {
        _createHandler = createHandler;
        _getHandler = getHandler;
        _approveHandler = approveHandler;
        _rejectHandler = rejectHandler;
    }

    [HttpGet]
    public async Task<IActionResult> GetResubmitRequests(
        [FromQuery] GetResubmitRequestsQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _getHandler.Handle(query, GetCurrentUserId(), cancellationToken);
            return Ok(new { success = true, data = response });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get resubmit requests.", error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateResubmitRequest(
        [FromBody] CreateResubmitRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _createHandler.Handle(request, GetCurrentUserId(), cancellationToken);
            return Ok(new { success = true, data = response });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to create resubmit request.", error = ex.Message });
        }
    }

    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(
        int id,
        [FromBody] ReviewResubmitRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _approveHandler.Handle(id, request, GetCurrentUserId(), cancellationToken);
            return Ok(new { success = true, data = response });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to approve resubmit request.", error = ex.Message });
        }
    }

    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject(
        int id,
        [FromBody] ReviewResubmitRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _rejectHandler.Handle(id, request, GetCurrentUserId(), cancellationToken);
            return Ok(new { success = true, data = response });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to reject resubmit request.", error = ex.Message });
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        throw new UnauthorizedAccessException("User is not authenticated.");
    }
}
