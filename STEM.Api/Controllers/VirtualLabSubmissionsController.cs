using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STEM.Application.Dtos.Simulation;
using STEM.Application.Interfaces;

namespace STEM.Api.Controllers;

[ApiController]
[Route("api/submissions/virtual-lab")]
[Authorize]
public class VirtualLabSubmissionsController : ControllerBase
{
    private readonly IVirtualLabRuntimeService _runtimeService;

    public VirtualLabSubmissionsController(IVirtualLabRuntimeService runtimeService)
    {
        _runtimeService = runtimeService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(VirtualLabSubmissionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Submit(
        [FromBody] VirtualLabSubmissionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _runtimeService.SubmitVirtualLabAsync(
                request,
                GetCurrentUserId(),
                cancellationToken);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
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
