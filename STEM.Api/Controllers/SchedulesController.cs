using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STEM.Application.Dtos.Schedules;
using STEM.Application.UseCases.Schedules;
using STEM.Core.Entities.Users;
using System.Security.Claims;

namespace STEM.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SchedulesController : ControllerBase
{
    private readonly CreateScheduleHandler _createScheduleHandler;
    private readonly UpdateScheduleHandler _updateScheduleHandler;
    private readonly DeleteScheduleHandler _deleteScheduleHandler;
    private readonly GetTeacherScheduleHandler _getTeacherScheduleHandler;
    private readonly GetStudentScheduleHandler _getStudentScheduleHandler;

    public SchedulesController(
        CreateScheduleHandler createScheduleHandler,
        UpdateScheduleHandler updateScheduleHandler,
        DeleteScheduleHandler deleteScheduleHandler,
        GetTeacherScheduleHandler getTeacherScheduleHandler,
        GetStudentScheduleHandler getStudentScheduleHandler)
    {
        _createScheduleHandler = createScheduleHandler;
        _updateScheduleHandler = updateScheduleHandler;
        _deleteScheduleHandler = deleteScheduleHandler;
        _getTeacherScheduleHandler = getTeacherScheduleHandler;
        _getStudentScheduleHandler = getStudentScheduleHandler;
    }

    [HttpPost]
    [Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> CreateSchedule(
        [FromBody] CreateScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var result = await _createScheduleHandler.Handle(request, currentUserId, cancellationToken);
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
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi tạo lịch.", error = ex.InnerException?.Message ?? ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> UpdateSchedule(
        int id,
        [FromBody] UpdateScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            request.Id = id;
            var currentUserId = GetCurrentUserId();
            var result = await _updateScheduleHandler.Handle(id, request, currentUserId, cancellationToken);
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
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi cập nhật lịch.", error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> DeleteSchedule(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            await _deleteScheduleHandler.Handle(id, currentUserId, cancellationToken);
            return Ok(new { success = true, message = "Xóa lịch thành công." });
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
            return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi xóa lịch.", error = ex.Message });
        }
    }

    [HttpGet("my-schedule")]
    public async Task<IActionResult> GetMySchedule(
        [FromQuery] GetScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var roleName = User.FindFirst(ClaimTypes.Role)?.Value 
                ?? User.FindFirst("role")?.Value 
                ?? User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;

            IEnumerable<ScheduleCalendarResponse> result;

            if (roleName == "Student")
            {
                result = await _getStudentScheduleHandler.Handle(request, currentUserId, cancellationToken);
            }
            else
            {
                result = await _getTeacherScheduleHandler.Handle(request, currentUserId, cancellationToken);
            }

            return Ok(new { success = true, data = result });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi lấy lịch.", error = ex.Message });
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            throw new UnauthorizedAccessException("Invalid user token.");
        return userId;
    }

    [HttpGet("class/{classId:int}")]
    [Authorize(Roles = "SchoolAdmin,Student")]
    public async Task<IActionResult> GetSchedulesByClass(
        int classId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _getStudentScheduleHandler.Handle(
                new GetScheduleRequest { ClassId = classId, FromDate = fromDate, ToDate = toDate },
                0, // Not used for admin
                cancellationToken,
                isAdmin: true
            );
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi lấy lịch.", error = ex.Message });
        }
    }

    private Core.Entities.Users.User? GetCurrentUser()
    {
        return HttpContext.Items["CurrentUser"] as Core.Entities.Users.User;
    }
}
