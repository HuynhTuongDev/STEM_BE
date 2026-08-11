using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STEM.Application.Dtos.VirtualLabs;
using STEM.Application.UseCases.VirtualLabs;

namespace STEM.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VirtualLabsController : ControllerBase
{
    private readonly CreateVirtualLabHandler _createVirtualLabHandler;
    private readonly GetVirtualLabsHandler _getVirtualLabsHandler;
    private readonly GetVirtualLabDetailHandler _getVirtualLabDetailHandler;
    private readonly UpdateVirtualLabHandler _updateVirtualLabHandler;
    private readonly DeleteVirtualLabHandler _deleteVirtualLabHandler;

    public VirtualLabsController(
        CreateVirtualLabHandler createVirtualLabHandler,
        GetVirtualLabsHandler getVirtualLabsHandler,
        GetVirtualLabDetailHandler getVirtualLabDetailHandler,
        UpdateVirtualLabHandler updateVirtualLabHandler,
        DeleteVirtualLabHandler deleteVirtualLabHandler)
    {
        _createVirtualLabHandler = createVirtualLabHandler;
        _getVirtualLabsHandler = getVirtualLabsHandler;
        _getVirtualLabDetailHandler = getVirtualLabDetailHandler;
        _updateVirtualLabHandler = updateVirtualLabHandler;
        _deleteVirtualLabHandler = deleteVirtualLabHandler;
    }

    [HttpGet]
    public async Task<IActionResult> GetVirtualLabs(
        [FromQuery] GetVirtualLabsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _getVirtualLabsHandler.Handle(request, GetCurrentUserId(), cancellationToken);
            return Ok(new { success = true, data = response });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get virtual labs.", error = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetVirtualLab(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _getVirtualLabDetailHandler.Handle(id, GetCurrentUserId(), cancellationToken);
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
            return StatusCode(500, new { success = false, message = "Failed to get virtual lab.", error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateVirtualLab(
        [FromBody] CreateVirtualLabRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _createVirtualLabHandler.Handle(request, GetCurrentUserId(), cancellationToken);
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
            return StatusCode(500, new { success = false, message = "Failed to create virtual lab.", error = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateVirtualLab(
        int id,
        [FromBody] UpdateVirtualLabRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _updateVirtualLabHandler.Handle(id, request, GetCurrentUserId(), cancellationToken);
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
            return StatusCode(500, new { success = false, message = "Failed to update virtual lab.", error = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteVirtualLab(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _deleteVirtualLabHandler.Handle(id, GetCurrentUserId(), cancellationToken);
            return Ok(new { success = true, message = "Virtual lab deleted successfully." });
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
            return StatusCode(500, new { success = false, message = "Failed to delete virtual lab.", error = ex.Message });
        }
    }

    /// <summary>
    /// Get virtual labs for student (enrolled classes)
    /// </summary>
    [HttpGet("student")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetVirtualLabsForStudent(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? classId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var response = await _getVirtualLabsHandler.HandleForStudent(currentUserId, pageNumber, pageSize, classId, cancellationToken);
            return Ok(new { success = true, data = response });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get virtual labs.", error = ex.Message });
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
