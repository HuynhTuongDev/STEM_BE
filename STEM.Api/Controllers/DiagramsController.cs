using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STEM.Application.Dtos.Simulation;
using STEM.Application.Interfaces;

namespace STEM.Api.Controllers;

[ApiController]
[Route("api/diagrams")]
[Authorize]
public class DiagramsController : ControllerBase
{
    private readonly IVirtualLabRuntimeService _runtimeService;

    public DiagramsController(IVirtualLabRuntimeService runtimeService)
    {
        _runtimeService = runtimeService;
    }

    [HttpGet("{sessionId}")]
    [ProducesResponseType(typeof(DiagramSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDiagram(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _runtimeService.GetDiagramAsync(sessionId, GetCurrentUserId(), cancellationToken);
            return response == null
                ? NotFound(new { message = "Diagram session not found." })
                : Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("{sessionId}")]
    [HttpPut("{sessionId}")]
    [ProducesResponseType(typeof(DiagramSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveDiagram(
        string sessionId,
        [FromBody] SaveDiagramRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _runtimeService.SaveDiagramAsync(sessionId, request, GetCurrentUserId(), cancellationToken);
            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
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
