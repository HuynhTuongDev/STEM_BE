using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STEM.Application.Dtos.Courses;
using STEM.Application.UseCases.Courses;
using System.Security.Claims;

namespace STEM.Api.Controllers;

/// <summary>
/// API quản lý danh sách khoá học.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CoursesController : ControllerBase
{
    private readonly GetCoursesListHandler _getCoursesListHandler;
    private readonly GetCourseDetailHandler _getCourseDetailHandler;
    private readonly CreateCourseHandler _createCourseHandler;
    private readonly UpdateCourseHandler _updateCourseHandler;
    private readonly DeleteCourseHandler _deleteCourseHandler;

    public CoursesController(
        GetCoursesListHandler getCoursesListHandler,
        GetCourseDetailHandler getCourseDetailHandler,
        CreateCourseHandler createCourseHandler,
        UpdateCourseHandler updateCourseHandler,
        DeleteCourseHandler deleteCourseHandler)
    {
        _getCoursesListHandler = getCoursesListHandler;
        _getCourseDetailHandler = getCourseDetailHandler;
        _createCourseHandler = createCourseHandler;
        _updateCourseHandler = updateCourseHandler;
        _deleteCourseHandler = deleteCourseHandler;
    }

    /// <summary>
    /// Lấy danh sách khoá học (có phân trang và tìm kiếm).
    /// - MasterAdmin: không có quyền truy cập (chỉ quản lý School accounts).
    /// - SchoolAdmin: xem toàn bộ khoá học trong trường của mình.
    /// - Teacher/Student: chỉ xem khoá học thuộc trường của mình.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCourses(
        [FromQuery] GetCoursesRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var result = await _getCoursesListHandler.Handle(request, currentUserId, cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while fetching courses.", error = ex.Message });
        }
    }

    /// <summary>
    /// Xem chi tiết 1 khoá học.
    /// - SchoolAdmin, Teacher, Student được xem (nếu cùng trường).
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCourse(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var result = await _getCourseDetailHandler.Handle(id, currentUserId, cancellationToken);
            if (result == null)
                return NotFound(new { success = false, message = "Course not found." });

            return Ok(new { success = true, data = result });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while fetching course detail.", error = ex.Message });
        }
    }

    /// <summary>
    /// Thêm mới khoá học.
    /// - Chỉ SchoolAdmin mới được phép.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> CreateCourse(
        [FromBody] CreateCourseRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var resultId = await _createCourseHandler.Handle(request, currentUserId, cancellationToken);
            return Ok(new { success = true, data = new { id = resultId } });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
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
            return StatusCode(500, new { success = false, message = "An error occurred while creating course.", error = ex.Message });
        }
    }

    /// <summary>
    /// Cập nhật khoá học.
    /// - Chỉ SchoolAdmin mới được phép.
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> UpdateCourse(
        int id,
        [FromBody] UpdateCourseRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var success = await _updateCourseHandler.Handle(id, request, currentUserId, cancellationToken);
            if (!success)
                return NotFound(new { success = false, message = "Course not found." });

            return Ok(new { success = true, message = "Course updated successfully." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while updating course.", error = ex.Message });
        }
    }

    /// <summary>
    /// Xóa khoá học.
    /// - Chỉ SchoolAdmin mới được phép.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> DeleteCourse(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var success = await _deleteCourseHandler.Handle(id, currentUserId, cancellationToken);
            if (!success)
                return NotFound(new { success = false, message = "Course not found." });

            return Ok(new { success = true, message = "Course deleted successfully." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while deleting course.", error = ex.Message });
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out int userId))
            return userId;
        throw new UnauthorizedAccessException("User is not authenticated properly.");
    }
}
