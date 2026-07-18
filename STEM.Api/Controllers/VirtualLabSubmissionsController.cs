using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using STEM.Application.Dtos.Simulation;
using STEM.Application.Interfaces;

namespace STEM.Api.Controllers;

[ApiController]
[Route("api/submissions/virtual-lab")]
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
                TryGetCurrentUserId(),
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
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    private int? TryGetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
