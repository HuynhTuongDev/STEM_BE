using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STEM.Application.Dtos.Users;
using STEM.Application.UseCases.Users;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;
using System.Security.Claims;

namespace STEM.Api.Controllers;

/// <summary>
/// API quản lý danh sách giáo viên.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "SchoolAdminOnly")]
public class TeachersController : ControllerBase
{
    private readonly GetUsersListHandler _getUsersListHandler;
    private readonly GetUserDetailHandler _getUserDetailHandler;
    private readonly UpdateTeacherHandler _updateTeacherHandler;
    private readonly DeleteTeacherHandler _deleteTeacherHandler;
    private readonly IUserRepository _userRepository;

    public TeachersController(
        GetUsersListHandler getUsersListHandler,
        GetUserDetailHandler getUserDetailHandler,
        UpdateTeacherHandler updateTeacherHandler,
        DeleteTeacherHandler deleteTeacherHandler,
        IUserRepository userRepository)
    {
        _getUsersListHandler = getUsersListHandler;
        _getUserDetailHandler = getUserDetailHandler;
        _updateTeacherHandler = updateTeacherHandler;
        _deleteTeacherHandler = deleteTeacherHandler;
        _userRepository = userRepository;
    }

    /// <summary>
    /// Lấy danh sách giáo viên (có phân trang và tìm kiếm).
    /// Chỉ SchoolAdmin mới được phép.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTeachers(
        [FromQuery] GetUsersRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            request.RoleId = 3; // Teacher
            var currentUserId = GetCurrentUserId();
            var result = await _getUsersListHandler.Handle(request, currentUserId, cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while fetching teachers.", error = ex.Message });
        }
    }

    /// <summary>
    /// Xem chi tiết 1 giáo viên.
    /// Chỉ SchoolAdmin mới được phép.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTeacher(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();

            // Verify current user is SchoolAdmin
            var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
            if (currentUser?.Role?.Name != RoleNames.SchoolAdministrator)
                return Forbid();

            var result = await _getUserDetailHandler.Handle(id, cancellationToken);

            if (result.RoleId != 3)
                return NotFound(new { success = false, message = "User is not a teacher." });

            if (result.SchoolId != currentUser.SchoolId)
                return NotFound(new { success = false, message = "Teacher not found in your school." });

            return Ok(new { success = true, data = result });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, message = "Teacher not found." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while fetching teacher detail.", error = ex.Message });
        }
    }

    /// <summary>
    /// Cập nhật thông tin giáo viên.
    /// Chỉ SchoolAdmin mới được phép.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTeacher(
        int id,
        [FromBody] UpdateTeacherRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            request.Id = id;
            var currentUserId = GetCurrentUserId();
            var success = await _updateTeacherHandler.Handle(request, currentUserId, cancellationToken);
            if (!success)
                return NotFound(new { success = false, message = "Teacher not found." });

            return Ok(new { success = true, message = "Teacher updated successfully." });
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { success = false, errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while updating teacher.", error = ex.Message });
        }
    }

    /// <summary>
    /// Xóa giáo viên.
    /// Chỉ SchoolAdmin mới được phép.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTeacher(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var success = await _deleteTeacherHandler.Handle(id, currentUserId, cancellationToken);
            if (!success)
                return NotFound(new { success = false, message = "Teacher not found." });

            return Ok(new { success = true, message = "Teacher deleted successfully." });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while deleting teacher.", error = ex.Message });
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
