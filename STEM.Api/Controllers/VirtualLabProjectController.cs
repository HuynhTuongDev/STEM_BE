using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STEM.Application.Dtos.VirtualLab;
using STEM.Application.Interfaces;

namespace STEM.Api.Controllers;

[ApiController]
[Route("api/virtual-lab/projects")]
public class VirtualLabProjectController : ControllerBase
{
    private readonly IVirtualLabProjectService _service;

    public VirtualLabProjectController(IVirtualLabProjectService service)
    {
        _service = service;
    }

    [HttpPost]
    [AllowAnonymous] // For MVP
    public async Task<IActionResult> CreateProject([FromBody] VirtualLabProjectRequest request)
    {
        try
        {
            // For MVP, we can leave userId as null
            var project = await _service.CreateProjectAsync(request, null);
            return Ok(project);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProject(Guid id)
    {
        var project = await _service.GetProjectAsync(id);
        if (project == null) return NotFound();

        return Ok(project);
    }

    [HttpPut("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> UpdateProject(Guid id, [FromBody] VirtualLabProjectRequest request)
    {
        try
        {
            var project = await _service.UpdateProjectAsync(id, request);
            if (project == null) return NotFound();

            return Ok(project);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/start")]
    [AllowAnonymous]
    public IActionResult StartSimulation(Guid id, [FromBody] StartSimulationRequest request)
    {
        // Mock Start Simulation (Task 29)
        return Ok(new
        {
            simulationId = Guid.NewGuid(),
            status = "running",
            logs = new[]
            {
                "Simulation started",
                "System initialized.",
                "BMP180 ready!",
                "Temperature: 24 C",
                "Pressure: 101325 Pa",
                "LED Pin configured as OUTPUT."
            }
        });
    }

    [HttpPost("{id}/stop")]
    [AllowAnonymous]
    public IActionResult StopSimulation(Guid id)
    {
        // Mock Stop Simulation (Task 30)
        return Ok(new
        {
            status = "stopped",
            logs = new[]
            {
                "Simulation stopped"
            }
        });
    }
}
