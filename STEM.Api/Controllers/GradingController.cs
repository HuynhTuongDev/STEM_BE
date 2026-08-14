using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STEM.Application.Dtos.Grading;
using STEM.Application.UseCases.Grading;

namespace STEM.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GradingController : ControllerBase
{
    private readonly GetSubmissionsHandler _getSubmissionsHandler;
    private readonly GetSubmissionDetailHandler _getSubmissionDetailHandler;
    private readonly GradeSubmissionHandler _gradeSubmissionHandler;
    private readonly UpdateSubmissionGradeHandler _updateSubmissionGradeHandler;
    private readonly GetSubmissionCommentsHandler _getSubmissionCommentsHandler;
    private readonly CreateSubmissionCommentHandler _createSubmissionCommentHandler;
    private readonly UpdateSubmissionCommentHandler _updateSubmissionCommentHandler;
    private readonly DeleteSubmissionCommentHandler _deleteSubmissionCommentHandler;

    public GradingController(
        GetSubmissionsHandler getSubmissionsHandler,
        GetSubmissionDetailHandler getSubmissionDetailHandler,
        GradeSubmissionHandler gradeSubmissionHandler,
        UpdateSubmissionGradeHandler updateSubmissionGradeHandler,
        GetSubmissionCommentsHandler getSubmissionCommentsHandler,
        CreateSubmissionCommentHandler createSubmissionCommentHandler,
        UpdateSubmissionCommentHandler updateSubmissionCommentHandler,
        DeleteSubmissionCommentHandler deleteSubmissionCommentHandler)
    {
        _getSubmissionsHandler = getSubmissionsHandler;
        _getSubmissionDetailHandler = getSubmissionDetailHandler;
        _gradeSubmissionHandler = gradeSubmissionHandler;
        _updateSubmissionGradeHandler = updateSubmissionGradeHandler;
        _getSubmissionCommentsHandler = getSubmissionCommentsHandler;
        _createSubmissionCommentHandler = createSubmissionCommentHandler;
        _updateSubmissionCommentHandler = updateSubmissionCommentHandler;
        _deleteSubmissionCommentHandler = deleteSubmissionCommentHandler;
    }

    [HttpGet("submissions")]
    public async Task<IActionResult> GetSubmissions(
        [FromQuery] GetSubmissionsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _getSubmissionsHandler.Handle(request, GetCurrentUserId(), cancellationToken);
            return Ok(new { success = true, data = response });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get submissions.", error = ex.Message });
        }
    }

    [HttpGet("submissions/{id:int}")]
    public async Task<IActionResult> GetSubmission(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _getSubmissionDetailHandler.Handle(id, GetCurrentUserId(), cancellationToken);
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
            return StatusCode(500, new { success = false, message = "Failed to get submission.", error = ex.Message });
        }
    }

    [HttpPost("submissions/{id:int}/grade")]
    public async Task<IActionResult> GradeSubmission(
        int id,
        [FromBody] GradeSubmissionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _gradeSubmissionHandler.Handle(id, request, GetCurrentUserId(), cancellationToken);
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
            return StatusCode(500, new { success = false, message = "Failed to grade submission.", error = ex.Message });
        }
    }

    [HttpPut("submissions/{id:int}/grade")]
    public async Task<IActionResult> UpdateSubmissionGrade(
        int id,
        [FromBody] GradeSubmissionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _updateSubmissionGradeHandler.Handle(id, request, GetCurrentUserId(), cancellationToken);
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
            return StatusCode(500, new { success = false, message = "Failed to update submission grade.", error = ex.Message });
        }
    }

    [HttpGet("submissions/{submissionId:int}/comments")]
    public async Task<IActionResult> GetComments(
        int submissionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _getSubmissionCommentsHandler.Handle(submissionId, GetCurrentUserId(), cancellationToken);
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
            return StatusCode(500, new { success = false, message = "Failed to get comments.", error = ex.Message });
        }
    }

    [HttpPost("submissions/{submissionId:int}/comments")]
    public async Task<IActionResult> CreateComment(
        int submissionId,
        [FromBody] SubmissionCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _createSubmissionCommentHandler.Handle(submissionId, request, GetCurrentUserId(), cancellationToken);
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
            return StatusCode(500, new { success = false, message = "Failed to create comment.", error = ex.Message });
        }
    }

    [HttpPut("submissions/{submissionId:int}/comments/{commentId:int}")]
    public async Task<IActionResult> UpdateComment(
        int submissionId,
        int commentId,
        [FromBody] SubmissionCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _updateSubmissionCommentHandler.Handle(submissionId, commentId, request, GetCurrentUserId(), cancellationToken);
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
            return StatusCode(500, new { success = false, message = "Failed to update comment.", error = ex.Message });
        }
    }

    [HttpDelete("submissions/{submissionId:int}/comments/{commentId:int}")]
    public async Task<IActionResult> DeleteComment(
        int submissionId,
        int commentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _deleteSubmissionCommentHandler.Handle(submissionId, commentId, GetCurrentUserId(), cancellationToken);
            return Ok(new { success = true });
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
            return StatusCode(500, new { success = false, message = "Failed to delete comment.", error = ex.Message });
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
