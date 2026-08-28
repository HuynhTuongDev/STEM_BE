using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STEM.Application.Dtos.Syllabuses;
using STEM.Application.UseCases.Syllabuses;
using System.Security.Claims;

namespace STEM.Api.Controllers;

/// <summary>
/// API quản lý Standard Syllabus (chương trình khung do Master Admin sở hữu).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SyllabusesController : ControllerBase
{
    private readonly GetSyllabusesListHandler _getSyllabusesListHandler;
    private readonly GetSyllabusDetailHandler _getSyllabusDetailHandler;
    private readonly GetSyllabusStructureHandler _getSyllabusStructureHandler;
    private readonly STEM.Application.UseCases.Curriculum.CreateSyllabusHandler _createSyllabusHandler;
    private readonly STEM.Application.UseCases.Curriculum.UpdateSyllabusHandler _updateSyllabusHandler;
    private readonly STEM.Application.UseCases.Curriculum.ArchiveSyllabusHandler _archiveSyllabusHandler;

    public SyllabusesController(
        GetSyllabusesListHandler getSyllabusesListHandler,
        GetSyllabusDetailHandler getSyllabusDetailHandler,
        GetSyllabusStructureHandler getSyllabusStructureHandler,
        STEM.Application.UseCases.Curriculum.CreateSyllabusHandler createSyllabusHandler,
        STEM.Application.UseCases.Curriculum.UpdateSyllabusHandler updateSyllabusHandler,
        STEM.Application.UseCases.Curriculum.ArchiveSyllabusHandler archiveSyllabusHandler)
    {
        _getSyllabusesListHandler = getSyllabusesListHandler;
        _getSyllabusDetailHandler = getSyllabusDetailHandler;
        _getSyllabusStructureHandler = getSyllabusStructureHandler;
        _createSyllabusHandler = createSyllabusHandler;
        _updateSyllabusHandler = updateSyllabusHandler;
        _archiveSyllabusHandler = archiveSyllabusHandler;
    }

    /// <summary>
    /// Lấy danh sách Standard Syllabus (có phân trang, tìm kiếm).
    /// Mọi role đã đăng nhập đều xem được (đây là chương trình khung dùng chung).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSyllabuses(
        [FromQuery] GetSyllabusesRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _getSyllabusesListHandler.Handle(request, cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while fetching syllabuses.", error = ex.Message });
        }
    }

    /// <summary>
    /// Xem chi tiết 1 Standard Syllabus.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetSyllabus(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _getSyllabusDetailHandler.Handle(id, cancellationToken);
            if (result == null)
                return NotFound(new { success = false, message = "Syllabus not found." });

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while fetching syllabus detail.", error = ex.Message });
        }
    }

    /// <summary>
    /// Lấy cấu trúc đầy đủ: Syllabus -> Course -> Module -> Lesson -> Lab (nếu có).
    /// </summary>
    [HttpGet("{id}/structure")]
    public async Task<IActionResult> GetSyllabusStructure(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _getSyllabusStructureHandler.Handle(id, cancellationToken);
            if (result == null)
                return NotFound(new { success = false, message = "Syllabus not found." });

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while fetching syllabus structure.", error = ex.Message });
        }
    }

    /// <summary>
    /// Tạo mới Standard Syllabus.
    /// - Chỉ Master Administrator mới được phép.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "MasterOnly")]
    public async Task<IActionResult> CreateSyllabus(
        [FromBody] STEM.Application.Dtos.Curriculum.CreateSyllabusRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var resultId = await _createSyllabusHandler.Handle(request, cancellationToken);
            return Ok(new { success = true, data = new { id = resultId } });
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
            return StatusCode(500, new { success = false, message = "An error occurred while creating syllabus.", error = ex.Message });
        }
    }

    /// <summary>
    /// Cập nhật Standard Syllabus.
    /// - Chỉ Master Administrator mới được phép.
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "MasterOnly")]
    public async Task<IActionResult> UpdateSyllabus(
        int id,
        [FromBody] STEM.Application.Dtos.Curriculum.UpdateSyllabusRequest request,
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
            return StatusCode(500, new { success = false, message = "An error occurred while updating syllabus.", error = ex.Message });
        }
    }

    /// <summary>
    /// Lưu trữ (archive) Standard Syllabus — không hard-delete.
    /// - Chỉ Master Administrator mới được phép.
    /// </summary>
    [HttpPost("{id}/archive")]
    [Authorize(Policy = "MasterOnly")]
    public async Task<IActionResult> ArchiveSyllabus(int id, CancellationToken cancellationToken = default)
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
            return StatusCode(500, new { success = false, message = "An error occurred while archiving syllabus.", error = ex.Message });
        }
    }
}
