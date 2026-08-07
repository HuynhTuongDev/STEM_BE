using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STEM.Application.Dtos.Assignments;
using STEM.Application.UseCases.Assignments;

namespace STEM.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AssignmentsController : ControllerBase
{
    private readonly CreateAssignmentHandler _createAssignmentHandler;
    private readonly GetAssignmentsHandler _getAssignmentsHandler;
    private readonly GetAssignmentDetailHandler _getAssignmentDetailHandler;
    private readonly UpdateAssignmentHandler _updateAssignmentHandler;
    private readonly DeleteAssignmentHandler _deleteAssignmentHandler;

    public AssignmentsController(
        CreateAssignmentHandler createAssignmentHandler,
        GetAssignmentsHandler getAssignmentsHandler,
        GetAssignmentDetailHandler getAssignmentDetailHandler,
        UpdateAssignmentHandler updateAssignmentHandler,
        DeleteAssignmentHandler deleteAssignmentHandler)
    {
        _createAssignmentHandler = createAssignmentHandler;
        _getAssignmentsHandler = getAssignmentsHandler;
        _getAssignmentDetailHandler = getAssignmentDetailHandler;
        _updateAssignmentHandler = updateAssignmentHandler;
        _deleteAssignmentHandler = deleteAssignmentHandler;
    }

    [HttpGet]
    public async Task<IActionResult> GetAssignments(
        [FromQuery] GetAssignmentsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _getAssignmentsHandler.Handle(request, GetCurrentUserId(), cancellationToken);
            return Ok(new { success = true, data = response });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get assignments.", error = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetAssignment(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _getAssignmentDetailHandler.Handle(id, GetCurrentUserId(), cancellationToken);
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
            return StatusCode(500, new { success = false, message = "Failed to get assignment.", error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateAssignment(
        [FromBody] CreateAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _createAssignmentHandler.Handle(request, GetCurrentUserId(), cancellationToken);
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
            return StatusCode(500, new { success = false, message = "Failed to create assignment.", error = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateAssignment(
        int id,
        [FromBody] UpdateAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _updateAssignmentHandler.Handle(id, request, GetCurrentUserId(), cancellationToken);
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
            return StatusCode(500, new { success = false, message = "Failed to update assignment.", error = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAssignment(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _deleteAssignmentHandler.Handle(id, GetCurrentUserId(), cancellationToken);
            return Ok(new { success = true, message = "Assignment deleted successfully." });
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
            return StatusCode(500, new { success = false, message = "Failed to delete assignment.", error = ex.Message });
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
