using System;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STEM.Application.Dtos.Simulation;
using STEM.Application.Dtos.VirtualLab;
using STEM.Application.Interfaces;

namespace STEM.Api.Controllers;

[ApiController]
[Route("api/virtual-lab/projects")]
[Authorize]
public class VirtualLabProjectController : ControllerBase
{
    private readonly IVirtualLabProjectService _service;
    private readonly IVirtualLabRuntimeService _runtimeService;

    public VirtualLabProjectController(
        IVirtualLabProjectService service,
        IVirtualLabRuntimeService runtimeService)
    {
        _service = service;
        _runtimeService = runtimeService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateProject([FromBody] VirtualLabProjectRequest request)
    {
        try
        {
            var project = await _service.CreateProjectAsync(request, GetCurrentUserId());
            return Ok(project);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProject(Guid id)
    {
        try
        {
            var project = await _service.GetProjectAsync(id, GetCurrentUserId());
            if (project == null) return NotFound();

            return Ok(project);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProject(Guid id, [FromBody] VirtualLabProjectRequest request)
    {
        try
        {
            var project = await _service.UpdateProjectAsync(id, request, GetCurrentUserId());
            if (project == null) return NotFound();

            return Ok(project);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/start")]
    public async Task<IActionResult> StartSimulation(
        Guid id,
        [FromBody] StartSimulationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _runtimeService.RunEsp32Async(new RunEsp32SimulationRequest
            {
                SessionId = id.ToString("N"),
                DiagramJson = ToDiagramJson(request.Diagram),
                SourceCode = request.Code,
                Mode = "mock"
            }, GetCurrentUserId(), cancellationToken);

            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/stop")]
    public async Task<IActionResult> StopSimulation(Guid id)
    {
        try
        {
            // stop is still a stub (no real state change yet — see Giai đoạn 4), but it must
            // still confirm the caller owns this project before confirming it "exists" at all.
            var project = await _service.GetProjectAsync(id, GetCurrentUserId());
            if (project == null) return NotFound();

            return Ok(new
            {
                sessionId = id.ToString("N"),
                status = "stopped",
                events = Array.Empty<SimulationEventResponse>()
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
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

    private static string ToDiagramJson(JsonElement diagram)
    {
        if (diagram.ValueKind == JsonValueKind.String)
        {
            return diagram.GetString() ?? "{}";
        }

        return diagram.ValueKind == JsonValueKind.Undefined
            ? "{}"
            : diagram.GetRawText();
    }
}
