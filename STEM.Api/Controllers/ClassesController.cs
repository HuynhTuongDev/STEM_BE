using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using STEM.Application.Dtos.Classes;
using STEM.Application.UseCases.Classes;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;
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
    private readonly GetStudentTemplateHandler _getStudentTemplateHandler;
    private readonly ImportStudentsHandler _importStudentsHandler;
    private readonly IUserRepository _userRepository;

    public ClassesController(
        GetClassesListHandler getClassesListHandler,
        GetClassDetailHandler getClassDetailHandler,
        CreateClassHandler createClassHandler,
        UpdateClassHandler updateClassHandler,
        DeleteClassHandler deleteClassHandler,
        AssignStudentsToClassHandler assignStudentsToClassHandler,
        RemoveStudentFromClassHandler removeStudentFromClassHandler,
        GetAvailableStudentsHandler getAvailableStudentsHandler,
        GetStudentTemplateHandler getStudentTemplateHandler,
        ImportStudentsHandler importStudentsHandler,
        IUserRepository userRepository)
    {
        _getClassesListHandler = getClassesListHandler;
        _getClassDetailHandler = getClassDetailHandler;
        _createClassHandler = createClassHandler;
        _updateClassHandler = updateClassHandler;
        _deleteClassHandler = deleteClassHandler;
        _assignStudentsToClassHandler = assignStudentsToClassHandler;
        _removeStudentFromClassHandler = removeStudentFromClassHandler;
        _getAvailableStudentsHandler = getAvailableStudentsHandler;
        _getStudentTemplateHandler = getStudentTemplateHandler;
        _importStudentsHandler = importStudentsHandler;
        _userRepository = userRepository;
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
    /// Remove student from class (POST method for FE compatibility)
    /// </summary>
    [HttpDelete("{classId}/students/remove")]
    [Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> RemoveStudentFromClassPost(int classId, [FromBody] RemoveStudentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            await _removeStudentFromClassHandler.Handle(classId, request.StudentId, currentUserId, cancellationToken);
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

    /// <summary>
    /// Download student template Excel file
    /// </summary>
    [HttpGet("{classId}/students/template")]
    [Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> GetStudentTemplate(
        int classId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var template = await _getStudentTemplateHandler.Handle(classId, currentUserId, cancellationToken);
            return Ok(new { success = true, data = template });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi lấy template.", error = ex.Message });
        }
    }

    /// <summary>
    /// Import students from Excel file
    /// </summary>
    [HttpPost("{classId}/students/import")]
    [Authorize(Policy = "SchoolAdminOnly")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> ImportStudents(
        int classId,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "Vui lòng chọn file Excel." });

            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) &&
                !file.FileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { success = false, message = "File phải là định dạng Excel (.xlsx, .xls)." });

            var currentUserId = GetCurrentUserId();
            var studentIds = new List<int>();

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream, cancellationToken);
            stream.Position = 0;

            using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1);
            var rows = worksheet.RangeUsed()?.RowsUsed().Skip(1); // Skip header row

            if (rows == null)
                return BadRequest(new { success = false, message = "File Excel không có dữ liệu." });

            foreach (var row in rows)
            {
                var idCell = row.Cell(1).Value;
                if (!idCell.IsBlank && int.TryParse(idCell.ToString(), out int studentId))
                {
                    studentIds.Add(studentId);
                }
            }

            if (!studentIds.Any())
                return BadRequest(new { success = false, message = "Không tìm thấy ID học sinh nào trong file." });

            var result = await _importStudentsHandler.Handle(classId, studentIds, currentUserId, cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi import học sinh.", error = ex.Message });
        }
    }

    /// <summary>
    /// Get my classes (for students and teachers)
    /// </summary>
    [HttpGet("my-classes")]
    [Authorize(Policy = "StudentAndAbove")]
    public async Task<IActionResult> GetMyClasses(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var roleName = GetCurrentUserRole();
            var request = new GetClassesRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Status = status
            };

            object result;
            if (roleName == RoleNames.Teacher)
            {
                result = await _getClassesListHandler.HandleTeacherClasses(currentUserId, request, cancellationToken);
            }
            else
            {
                result = await _getClassesListHandler.HandleStudentClasses(currentUserId, request, cancellationToken);
            }
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

    /// <summary>
    /// Get class detail for student or teacher
    /// </summary>
    [HttpGet("{classId}/detail")]
    [Authorize(Policy = "StudentAndAbove")]
    public async Task<IActionResult> GetClassDetailForStudent(
        int classId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var currentUser = await _userRepository.GetByIdAsync(currentUserId);
            var roleName = currentUser?.Role?.Name;

            object result;
            if (roleName == RoleNames.Student)
            {
                result = await _getClassDetailHandler.HandleForStudent(classId, currentUserId, cancellationToken);
            }
            else
            {
                result = await _getClassDetailHandler.HandleForTeacher(classId, currentUserId, cancellationToken);
            }
            return Ok(new { success = true, data = result });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
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

    /// <summary>
    /// Get class schedule for student
    /// </summary>
    [HttpGet("{classId}/schedule")]
    [Authorize(Roles = RoleNames.Student)]
    public async Task<IActionResult> GetClassScheduleForStudent(
        int classId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var result = await _getClassDetailHandler.HandleGetScheduleForStudent(classId, currentUserId, fromDate, toDate, cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi lấy lịch học.", error = ex.Message });
        }
    }

    /// <summary>
    /// Get modules and lessons for a class (for teachers and students)
    /// </summary>
    [HttpGet("{classId}/curriculum")]
    [Authorize(Policy = "StudentAndAbove")]
    public async Task<IActionResult> GetClassCurriculum(
        int classId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var result = await _getClassDetailHandler.HandleGetCurriculum(classId, currentUserId, cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi lấy giáo trình.", error = ex.Message });
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out int userId))
            return userId;
        throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");
    }

    private string GetCurrentUserRole()
    {
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
        return roleClaim ?? string.Empty;
    }
}
