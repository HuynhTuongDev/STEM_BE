using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STEM.Application.Dtos.Simulation;
using STEM.Application.Interfaces;

namespace STEM.Api.Controllers;

[ApiController]
[Route("api/simulation")]
[Authorize]
public class SimulationCompileController : ControllerBase
{
    private readonly ISimulationCompileService _compileService;

    public SimulationCompileController(ISimulationCompileService compileService)
    {
        _compileService = compileService;
    }

    [HttpPost("compile")]
    public async Task<IActionResult> Compile(
        [FromBody] CompileSimulationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _compileService.CompileAsync(request, GetCurrentUserId(), cancellationToken);
            return Ok(new { success = response.Success, data = response });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to compile simulation code.", error = ex.Message });
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
