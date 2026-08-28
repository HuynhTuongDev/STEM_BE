using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STEM.Application.Dtos.Labs;
using STEM.Application.Interfaces;

namespace STEM.Api.Controllers;

// Lab-centric submission API — Assignment là chi tiết triển khai nội bộ
// (hidden), FE/Teacher không cần biết AssignmentId. Xem
// STEM.Infrastructure/Services/LabSubmissionService.cs.
[ApiController]
[Route("api/labs/{labId:guid}")]
[Authorize]
public class LabSubmissionsController : ControllerBase
{
    private readonly ILabSubmissionService _labSubmissionService;

    public LabSubmissionsController(ILabSubmissionService labSubmissionService)
    {
        _labSubmissionService = labSubmissionService;
    }

    [HttpGet("submissions")]
    public async Task<IActionResult> GetSubmissions(
        Guid labId,
        [FromQuery] int classId,
        CancellationToken cancellationToken = default)
    {
        if (classId <= 0)
        {
            return BadRequest(new { success = false, message = "classId is required." });
        }

        try
        {
            var response = await _labSubmissionService.GetSubmissionsAsync(labId, classId, GetCurrentUserId(), cancellationToken);
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
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get lab submissions.", error = ex.Message });
        }
    }

    [HttpPost("submit")]
    public async Task<IActionResult> Submit(
        Guid labId,
        [FromBody] SubmitLabRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _labSubmissionService.SubmitAsync(labId, request, GetCurrentUserId(), cancellationToken);
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
            return StatusCode(500, new { success = false, message = "Failed to submit lab.", error = ex.Message });
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
