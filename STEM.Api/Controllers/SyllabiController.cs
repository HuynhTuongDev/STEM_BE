using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STEM.Application.Dtos.Curriculum;
using STEM.Application.UseCases.Curriculum;

namespace STEM.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SyllabiController : ControllerBase
{
    private readonly GetSyllabiHandler _getSyllabiHandler;
    private readonly GetSyllabusByIdHandler _getSyllabusByIdHandler;
    private readonly CreateSyllabusHandler _createSyllabusHandler;
    private readonly UpdateSyllabusHandler _updateSyllabusHandler;
    private readonly DeleteSyllabusHandler _deleteSyllabusHandler;
    private readonly PublishSyllabusHandler _publishSyllabusHandler;
    private readonly ArchiveSyllabusHandler _archiveSyllabusHandler;
    private readonly UnpublishSyllabusHandler _unpublishSyllabusHandler;
    private readonly RestoreSyllabusHandler _restoreSyllabusHandler;

    public SyllabiController(
        GetSyllabiHandler getSyllabiHandler,
        GetSyllabusByIdHandler getSyllabusByIdHandler,
        CreateSyllabusHandler createSyllabusHandler,
        UpdateSyllabusHandler updateSyllabusHandler,
        DeleteSyllabusHandler deleteSyllabusHandler,
        PublishSyllabusHandler publishSyllabusHandler,
        ArchiveSyllabusHandler archiveSyllabusHandler,
        UnpublishSyllabusHandler unpublishSyllabusHandler,
        RestoreSyllabusHandler restoreSyllabusHandler)
    {
        _getSyllabiHandler = getSyllabiHandler;
        _getSyllabusByIdHandler = getSyllabusByIdHandler;
        _createSyllabusHandler = createSyllabusHandler;
        _updateSyllabusHandler = updateSyllabusHandler;
        _deleteSyllabusHandler = deleteSyllabusHandler;
        _publishSyllabusHandler = publishSyllabusHandler;
        _archiveSyllabusHandler = archiveSyllabusHandler;
        _unpublishSyllabusHandler = unpublishSyllabusHandler;
        _restoreSyllabusHandler = restoreSyllabusHandler;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] int? gradeLevelId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _getSyllabiHandler.Handle(status, gradeLevelId, cancellationToken);
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
            var result = await _getSyllabusByIdHandler.Handle(id, cancellationToken);
            if (result == null)
                return NotFound(new { success = false, message = "Syllabus not found." });

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
        [FromBody] CreateSyllabusRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var id = await _createSyllabusHandler.Handle(request, cancellationToken);
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
        [FromBody] UpdateSyllabusRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _updateSyllabusHandler.Handle(id, request, cancellationToken);
            if (!success)
                return NotFound(new { success = false, message = "Syllabus not found." });

            return Ok(new { success = true, message = "Syllabus updated successfully." });
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
            var success = await _deleteSyllabusHandler.Handle(id, cancellationToken);
            if (!success)
                return NotFound(new { success = false, message = "Syllabus not found." });

            return Ok(new { success = true, message = "Syllabus deleted successfully." });
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

    [HttpPost("{id}/publish")]
    [Authorize(Policy = "MasterAdminOnly")]
    public async Task<IActionResult> Publish(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _publishSyllabusHandler.Handle(id, cancellationToken);
            if (!success)
                return NotFound(new { success = false, message = "Syllabus not found." });

            return Ok(new { success = true, message = "Syllabus published successfully." });
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

    [HttpPost("{id}/archive")]
    [Authorize(Policy = "MasterAdminOnly")]
    public async Task<IActionResult> Archive(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _archiveSyllabusHandler.Handle(id, cancellationToken);
            if (!success)
                return NotFound(new { success = false, message = "Syllabus not found." });

            return Ok(new { success = true, message = "Syllabus archived successfully." });
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

    [HttpPost("{id}/unpublish")]
    [Authorize(Policy = "MasterAdminOnly")]
    public async Task<IActionResult> Unpublish(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _unpublishSyllabusHandler.Handle(id, cancellationToken);
            if (!success)
                return NotFound(new { success = false, message = "Syllabus not found." });

            return Ok(new { success = true, message = "Syllabus unpublished successfully." });
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

    [HttpPost("{id}/restore")]
    [Authorize(Policy = "MasterAdminOnly")]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _restoreSyllabusHandler.Handle(id, cancellationToken);
            if (!success)
                return NotFound(new { success = false, message = "Syllabus not found." });

            return Ok(new { success = true, message = "Syllabus restored successfully." });
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
}
