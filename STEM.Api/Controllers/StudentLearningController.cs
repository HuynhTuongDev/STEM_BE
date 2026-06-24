using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STEM.Application.Dtos.StudentLearning;
using STEM.Application.UseCases.StudentLearning;
using STEM.Core.Entities.Users;

namespace STEM.Api.Controllers;

[ApiController]
[Route("api/student")]
[Authorize(Roles = RoleNames.Student)]
public class StudentLearningController : ControllerBase
{
    private readonly GetStudentAssignmentsHandler _getStudentAssignmentsHandler;
    private readonly SubmitAssignmentHandler _submitAssignmentHandler;
    private readonly GetSubmissionStatusHandler _getSubmissionStatusHandler;
    private readonly GetStudentQuizzesHandler _getStudentQuizzesHandler;
    private readonly GetStudentQuizDetailHandler _getStudentQuizDetailHandler;
    private readonly SubmitQuizAttemptHandler _submitQuizAttemptHandler;
    private readonly GetQuizResultHandler _getQuizResultHandler;

    public StudentLearningController(
        GetStudentAssignmentsHandler getStudentAssignmentsHandler,
        SubmitAssignmentHandler submitAssignmentHandler,
        GetSubmissionStatusHandler getSubmissionStatusHandler,
        GetStudentQuizzesHandler getStudentQuizzesHandler,
        GetStudentQuizDetailHandler getStudentQuizDetailHandler,
        SubmitQuizAttemptHandler submitQuizAttemptHandler,
        GetQuizResultHandler getQuizResultHandler)
    {
        _getStudentAssignmentsHandler = getStudentAssignmentsHandler;
        _submitAssignmentHandler = submitAssignmentHandler;
        _getSubmissionStatusHandler = getSubmissionStatusHandler;
        _getStudentQuizzesHandler = getStudentQuizzesHandler;
        _getStudentQuizDetailHandler = getStudentQuizDetailHandler;
        _submitQuizAttemptHandler = submitQuizAttemptHandler;
        _getQuizResultHandler = getQuizResultHandler;
    }

    [HttpGet("assignments")]
    public async Task<IActionResult> GetAssignments(
        [FromQuery] GetStudentAssignmentsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _getStudentAssignmentsHandler.Handle(request, GetCurrentUserId(), cancellationToken);
            return Ok(new { success = true, data = result });
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

    [HttpPost("assignments/{assignmentId:int}/submissions")]
    public async Task<IActionResult> SubmitAssignment(
        int assignmentId,
        [FromBody] SubmitAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _submitAssignmentHandler.Handle(assignmentId, request, GetCurrentUserId(), cancellationToken);
            return Ok(new { success = true, data = result });
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
            return StatusCode(500, new { success = false, message = "Failed to submit assignment.", error = ex.Message });
        }
    }

    [HttpGet("assignments/{assignmentId:int}/submission-status")]
    public async Task<IActionResult> GetSubmissionStatus(
        int assignmentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _getSubmissionStatusHandler.Handle(assignmentId, GetCurrentUserId(), cancellationToken);
            return Ok(new { success = true, data = result });
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
            return StatusCode(500, new { success = false, message = "Failed to get submission status.", error = ex.Message });
        }
    }

    [HttpGet("quizzes")]
    [HttpGet("tests")]
    public async Task<IActionResult> GetQuizzes(
        [FromQuery] GetStudentQuizzesRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _getStudentQuizzesHandler.Handle(request, GetCurrentUserId(), cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get quizzes.", error = ex.Message });
        }
    }

    [HttpGet("quizzes/{quizId:int}")]
    [HttpGet("tests/{quizId:int}")]
    public async Task<IActionResult> GetQuiz(
        int quizId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _getStudentQuizDetailHandler.Handle(quizId, GetCurrentUserId(), cancellationToken);
            return Ok(new { success = true, data = result });
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
            return StatusCode(500, new { success = false, message = "Failed to get quiz.", error = ex.Message });
        }
    }

    [HttpPost("quizzes/{quizId:int}/attempts")]
    [HttpPost("tests/{quizId:int}/attempts")]
    public async Task<IActionResult> SubmitQuizAttempt(
        int quizId,
        [FromBody] SubmitQuizAttemptRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _submitQuizAttemptHandler.Handle(quizId, request, GetCurrentUserId(), cancellationToken);
            return Ok(new { success = true, data = result });
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
            return StatusCode(500, new { success = false, message = "Failed to submit quiz attempt.", error = ex.Message });
        }
    }

    [HttpGet("quizzes/{quizId:int}/result")]
    [HttpGet("tests/{quizId:int}/result")]
    public async Task<IActionResult> GetQuizResult(
        int quizId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _getQuizResultHandler.Handle(quizId, GetCurrentUserId(), cancellationToken);
            if (result == null)
            {
                return NotFound(new { success = false, message = "Quiz result not found." });
            }

            return Ok(new { success = true, data = result });
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
            return StatusCode(500, new { success = false, message = "Failed to get quiz result.", error = ex.Message });
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        throw new UnauthorizedAccessException("User is not authenticated properly.");
    }
}
