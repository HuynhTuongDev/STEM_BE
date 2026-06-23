using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STEM.Application.Dtos.Attendance;
using STEM.Application.UseCases.Attendance;

namespace STEM.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly CreateAttendanceHandler _createAttendanceHandler;
    private readonly UpdateAttendanceHandler _updateAttendanceHandler;
    private readonly GetAttendanceHandler _getAttendanceHandler;

    public AttendanceController(
        CreateAttendanceHandler createAttendanceHandler,
        UpdateAttendanceHandler updateAttendanceHandler,
        GetAttendanceHandler getAttendanceHandler)
    {
        _createAttendanceHandler = createAttendanceHandler;
        _updateAttendanceHandler = updateAttendanceHandler;
        _getAttendanceHandler = getAttendanceHandler;
    }

    [HttpGet]
    public async Task<IActionResult> GetAttendance(
        [FromQuery] GetAttendanceRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var response = await _getAttendanceHandler.Handle(request, currentUserId, cancellationToken);
            return Ok(new { success = true, data = response });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get attendance.", error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateAttendance(
        [FromBody] CreateAttendanceRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var response = await _createAttendanceHandler.Handle(request, currentUserId, cancellationToken);
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
            return StatusCode(500, new { success = false, message = "Failed to create attendance.", error = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateAttendance(
        int id,
        [FromBody] UpdateAttendanceRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var response = await _updateAttendanceHandler.Handle(id, request, currentUserId, cancellationToken);
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
            return StatusCode(500, new { success = false, message = "Failed to update attendance.", error = ex.Message });
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
