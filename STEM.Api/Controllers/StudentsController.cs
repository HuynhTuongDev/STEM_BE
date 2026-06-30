using Microsoft.AspNetCore.Mvc;
using STEM.Application.Dtos.Students;
using STEM.Application.UseCases.Students;

namespace STEM.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentsController : ControllerBase
{
    private readonly GetStudentsHandler _getStudentsHandler;
    private readonly GetStudentLearningProgressHandler _getStudentLearningProgressHandler;

    public StudentsController(
        GetStudentsHandler getStudentsHandler,
        GetStudentLearningProgressHandler getStudentLearningProgressHandler)
    {
        _getStudentsHandler = getStudentsHandler;
        _getStudentLearningProgressHandler = getStudentLearningProgressHandler;
    }

    /// <summary>
    /// Get paged student list.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(StudentsListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetStudents(
        [FromQuery] GetStudentsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _getStudentsHandler.Handle(request, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get learning progress for a specific student.
    /// </summary>
    [HttpGet("{studentId:int}/learning-progress")]
    [ProducesResponseType(typeof(StudentLearningProgressResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetLearningProgress(
        int studentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _getStudentLearningProgressHandler.Handle(studentId, cancellationToken);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
