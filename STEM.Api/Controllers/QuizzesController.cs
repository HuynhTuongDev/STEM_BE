using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STEM.Application.Dtos.Quizzes;
using STEM.Application.UseCases.Quizzes;

namespace STEM.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QuizzesController : ControllerBase
{
    private readonly CreateQuizHandler _createQuizHandler;
    private readonly GetQuizzesHandler _getQuizzesHandler;
    private readonly GetQuizDetailHandler _getQuizDetailHandler;
    private readonly UpdateQuizHandler _updateQuizHandler;
    private readonly DeleteQuizHandler _deleteQuizHandler;

    public QuizzesController(
        CreateQuizHandler createQuizHandler,
        GetQuizzesHandler getQuizzesHandler,
        GetQuizDetailHandler getQuizDetailHandler,
        UpdateQuizHandler updateQuizHandler,
        DeleteQuizHandler deleteQuizHandler)
    {
        _createQuizHandler = createQuizHandler;
        _getQuizzesHandler = getQuizzesHandler;
        _getQuizDetailHandler = getQuizDetailHandler;
        _updateQuizHandler = updateQuizHandler;
        _deleteQuizHandler = deleteQuizHandler;
    }

    [HttpGet]
    public async Task<IActionResult> GetQuizzes(
        [FromQuery] GetQuizzesRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _getQuizzesHandler.Handle(request, GetCurrentUserId(), cancellationToken);
            return Ok(new { success = true, data = response });
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

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetQuiz(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _getQuizDetailHandler.Handle(id, GetCurrentUserId(), cancellationToken);
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
            return StatusCode(500, new { success = false, message = "Failed to get quiz.", error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateQuiz(
        [FromBody] CreateQuizRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _createQuizHandler.Handle(request, GetCurrentUserId(), cancellationToken);
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
            return StatusCode(500, new { success = false, message = "Failed to create quiz.", error = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateQuiz(
        int id,
        [FromBody] UpdateQuizRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _updateQuizHandler.Handle(id, request, GetCurrentUserId(), cancellationToken);
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
            return StatusCode(500, new { success = false, message = "Failed to update quiz.", error = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteQuiz(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _deleteQuizHandler.Handle(id, GetCurrentUserId(), cancellationToken);
            return Ok(new { success = true, message = "Quiz deleted successfully." });
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
            return StatusCode(500, new { success = false, message = "Failed to delete quiz.", error = ex.Message });
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
