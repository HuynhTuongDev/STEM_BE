using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STEM.Application.Dtos.Labs;
using STEM.Application.Interfaces;

namespace STEM.Api.Controllers;

[ApiController]
[Route("api/labs")]
[Authorize]
public class LabsController : ControllerBase
{
    private readonly ILabService _labService;

    public LabsController(ILabService labService)
    {
        _labService = labService;
    }

    [HttpGet]
    public async Task<IActionResult> GetLabs(
        [FromQuery] GetLabsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _labService.GetLabsAsync(request, GetCurrentUserId(), cancellationToken);
            return Ok(new { success = true, data = response });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get labs.", error = ex.Message });
        }
    }

    [HttpGet("component-glue-registry")]
    public async Task<IActionResult> GetComponentGlueRegistry(
        [FromQuery] bool supportedOnly = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _labService.GetComponentGlueRegistryAsync(supportedOnly, cancellationToken);
            return Ok(new { success = true, data = response });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get component glue registry.", error = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetLab(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _labService.GetLabAsync(id, GetCurrentUserId(), cancellationToken);
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
            return StatusCode(500, new { success = false, message = "Failed to get lab.", error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateLab(
        [FromBody] CreateLabRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _labService.CreateLabAsync(request, GetCurrentUserId(), cancellationToken);
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
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to create lab.", error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateLab(
        Guid id,
        [FromBody] UpdateLabRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _labService.UpdateLabAsync(id, request, GetCurrentUserId(), cancellationToken);
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
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to update lab.", error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteLab(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _labService.DeleteLabAsync(id, GetCurrentUserId(), cancellationToken);
            return Ok(new { success = true, message = "Lab deleted successfully." });
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
            return StatusCode(500, new { success = false, message = "Failed to delete lab.", error = ex.Message });
        }
    }

    [HttpPost("validate-wokwi")]
    public async Task<IActionResult> ValidateWokwiProject(
        [FromBody] ValidateWokwiProjectRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _labService.ValidateWokwiProjectAsync(request, cancellationToken);
            return Ok(new { success = true, data = response });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to validate Wokwi project.", error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/validate-wokwi")]
    public async Task<IActionResult> ValidateExistingWokwiProject(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _labService.ValidateExistingWokwiProjectAsync(id, GetCurrentUserId(), cancellationToken);
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
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to validate Wokwi project.", error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/progress/start")]
    public async Task<IActionResult> StartProgress(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _labService.StartProgressAsync(id, GetCurrentUserId(), cancellationToken);
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
            return StatusCode(500, new { success = false, message = "Failed to start lab progress.", error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/progress/complete")]
    public async Task<IActionResult> CompleteProgress(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _labService.CompleteProgressAsync(id, GetCurrentUserId(), cancellationToken);
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
            return StatusCode(500, new { success = false, message = "Failed to complete lab progress.", error = ex.Message });
        }
    }

    [HttpGet("{id:guid}/stats")]
    public async Task<IActionResult> GetStats(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _labService.GetStatsAsync(id, GetCurrentUserId(), cancellationToken);
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
            return StatusCode(500, new { success = false, message = "Failed to get lab stats.", error = ex.Message });
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
