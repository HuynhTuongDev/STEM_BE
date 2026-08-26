using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STEM.Application.Dtos.Curriculum;
using STEM.Application.UseCases.Curriculum;

namespace STEM.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LessonsController : ControllerBase
{
    private readonly GetLessonsHandler _getLessonsHandler;
    private readonly GetLessonByIdHandler _getLessonByIdHandler;
    private readonly CreateLessonHandler _createLessonHandler;
    private readonly UpdateLessonHandler _updateLessonHandler;
    private readonly DeleteLessonHandler _deleteLessonHandler;
    private readonly ReorderLessonsHandler _reorderLessonsHandler;

    public LessonsController(
        GetLessonsHandler getLessonsHandler,
        GetLessonByIdHandler getLessonByIdHandler,
        CreateLessonHandler createLessonHandler,
        UpdateLessonHandler updateLessonHandler,
        DeleteLessonHandler deleteLessonHandler,
        ReorderLessonsHandler reorderLessonsHandler)
    {
        _getLessonsHandler = getLessonsHandler;
        _getLessonByIdHandler = getLessonByIdHandler;
        _createLessonHandler = createLessonHandler;
        _updateLessonHandler = updateLessonHandler;
        _deleteLessonHandler = deleteLessonHandler;
        _reorderLessonsHandler = reorderLessonsHandler;
    }

    [HttpGet("by-module/{moduleId}")]
    public async Task<IActionResult> GetByModule(int moduleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _getLessonsHandler.Handle(moduleId, cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("by-class/{classId}")]
    public async Task<IActionResult> GetByClass(int classId, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _getLessonsHandler.HandleByClass(classId, cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _getLessonByIdHandler.Handle(id, cancellationToken);
            if (result == null)
                return NotFound(new { success = false, message = "Lesson not found." });

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Policy = "MasterAdminOnly")]
    public async Task<IActionResult> Create(
        [FromBody] CreateLessonRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var id = await _createLessonHandler.Handle(request, cancellationToken);
            return Ok(new { success = true, data = new { id } });
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
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "MasterAdminOnly")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateLessonRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _updateLessonHandler.Handle(id, request, cancellationToken);
            if (!success)
                return NotFound(new { success = false, message = "Lesson not found." });

            return Ok(new { success = true, message = "Lesson updated successfully." });
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
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "MasterAdminOnly")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _deleteLessonHandler.Handle(id, cancellationToken);
            if (!success)
                return NotFound(new { success = false, message = "Lesson not found." });

            return Ok(new { success = true, message = "Lesson deleted successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("reorder")]
    [Authorize(Policy = "MasterAdminOnly")]
    public async Task<IActionResult> Reorder(
        [FromBody] ReorderLessonsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _reorderLessonsHandler.Handle(request, cancellationToken);
            return Ok(new { success = true, message = "Lessons reordered successfully." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
