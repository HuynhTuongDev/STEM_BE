using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using STEM.Application.Dtos.Classes;
using STEM.Application.UseCases.Classes;
using STEM.Core.Entities.Users;
using System.Security.Claims;

namespace STEM.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClassesController : ControllerBase
{
    private readonly GetClassesListHandler _getClassesListHandler;
    private readonly GetClassDetailHandler _getClassDetailHandler;
    private readonly CreateClassHandler _createClassHandler;
    private readonly UpdateClassHandler _updateClassHandler;
    private readonly DeleteClassHandler _deleteClassHandler;
    private readonly AssignStudentsToClassHandler _assignStudentsToClassHandler;
    private readonly RemoveStudentFromClassHandler _removeStudentFromClassHandler;
    private readonly GetAvailableStudentsHandler _getAvailableStudentsHandler;

    public ClassesController(
        GetClassesListHandler getClassesListHandler,
        GetClassDetailHandler getClassDetailHandler,
        CreateClassHandler createClassHandler,
        UpdateClassHandler updateClassHandler,
        DeleteClassHandler deleteClassHandler,
        AssignStudentsToClassHandler assignStudentsToClassHandler,
        RemoveStudentFromClassHandler removeStudentFromClassHandler,
        GetAvailableStudentsHandler getAvailableStudentsHandler)
    {
        _getClassesListHandler = getClassesListHandler;
        _getClassDetailHandler = getClassDetailHandler;
        _createClassHandler = createClassHandler;
        _updateClassHandler = updateClassHandler;
        _deleteClassHandler = deleteClassHandler;
        _assignStudentsToClassHandler = assignStudentsToClassHandler;
        _removeStudentFromClassHandler = removeStudentFromClassHandler;
        _getAvailableStudentsHandler = getAvailableStudentsHandler;
    }

    [HttpGet]
    public async Task<IActionResult> GetClasses(
        [FromQuery] GetClassesRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var result = await _getClassesListHandler.Handle(request, currentUserId, cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi lấy danh sách lớp học.", error = ex.Message });
        }
    }

    [HttpGet("my-classes/{id:int}")]
    [Authorize(Roles = RoleNames.Teacher)]
    public async Task<IActionResult> GetMyClasses(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (id <= 0)
                return BadRequest(new { success = false, message = "TeacherId is required." });

            if (id != currentUserId)
                return Forbid();

            var result = await _getClassesListHandler.HandleTeacherClasses(id, currentUserId, cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "ÄÃ£ xáº£y ra lá»—i khi láº¥y danh sÃ¡ch lá»›p há»c cá»§a giÃ¡o viÃªn.", error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetClass(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var result = await _getClassDetailHandler.Handle(id, currentUserId, cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, message = "Không tìm thấy lớp học." });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi lấy chi tiết lớp học.", error = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> CreateClass(
        [FromBody] CreateClassRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var resultId = await _createClassHandler.Handle(request, currentUserId, cancellationToken);
            return Ok(new { success = true, data = new { id = resultId } });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (DbUpdateException ex)
        {
            var inner = ex.InnerException?.Message ?? ex.Message;
            return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi tạo lớp học.", error = inner });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi tạo lớp học.", error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> UpdateClass(
        int id,
        [FromBody] UpdateClassRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var success = await _updateClassHandler.Handle(id, request, currentUserId, cancellationToken);
            if (!success)
                return NotFound(new { success = false, message = "Không tìm thấy lớp học." });

            return Ok(new { success = true, message = "Cập nhật lớp học thành công." });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi cập nhật lớp học.", error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> DeleteClass(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var success = await _deleteClassHandler.Handle(id, currentUserId, cancellationToken);
            if (!success)
                return NotFound(new { success = false, message = "Không tìm thấy lớp học." });

            return Ok(new { success = true, message = "Xóa lớp học thành công." });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi xóa lớp học.", error = ex.Message });
        }
    }

    [HttpPost("{classId}/assign-students")]
    [Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> AssignStudentsToClass(int classId, [FromBody] AssignStudentsRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var result = await _assignStudentsToClassHandler.Handle(classId, request, currentUserId);
            
            if (result.AlreadyEnrolledCount > 0 && result.SuccessCount == 0)
            {
                return Ok(new { 
                    success = true, 
                    message = $"Tất cả học sinh đã được thêm vào lớp trước đó.",
                    alreadyEnrolled = result.AlreadyEnrolledCount
                });
            }
            
            return Ok(new { 
                success = true, 
                message = $"Đã thêm {result.SuccessCount} học sinh vào lớp.",
                data = result
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi thêm học sinh vào lớp.", error = ex.Message });
        }
    }

    [HttpDelete("{classId}/students/{studentId}")]
    [Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> RemoveStudentFromClass(int classId, int studentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            await _removeStudentFromClassHandler.Handle(classId, studentId, currentUserId, cancellationToken);
            return Ok(new { success = true, message = "Đã xóa học sinh khỏi lớp thành công." });
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
            return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi xóa học sinh khỏi lớp.", error = ex.Message });
        }
    }

    /// <summary>
    /// Get available students for a class (students who can be added without schedule conflicts)
    /// </summary>
    [HttpGet("{classId}/available-students")]
    [Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> GetAvailableStudents(
        int classId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var request = new AvailableStudentsRequest
            {
                Page = page,
                PageSize = pageSize,
                Search = search
            };
            var result = await _getAvailableStudentsHandler.Handle(classId, currentUserId, request);
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
            return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi lấy danh sách học sinh.", error = ex.Message });
        }
    }

    /// <summary>
    /// Get available teachers for a class (teachers who can be assigned without schedule conflicts)
    /// </summary>
    [HttpGet("{classId}/available-teachers")]
    [Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> GetAvailableTeachers(
        int classId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var result = await _getAvailableStudentsHandler.HandleGetAvailableTeachers(classId, cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi lấy danh sách giáo viên.", error = ex.Message });
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out int userId))
            return userId;
        throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");
    }
}
