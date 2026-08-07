using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using STEM.Application.Dtos.Students;
using STEM.Application.Dtos.Schedules;
using STEM.Application.UseCases.Students;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "SchoolAdminOnly")]
public class StudentsController : ControllerBase
{
    private readonly GetStudentsHandler _getStudentsHandler;
    private readonly GetStudentLearningProgressHandler _getStudentLearningProgressHandler;
    private readonly CreateStudentHandler _createStudentHandler;
    private readonly UpdateStudentHandler _updateStudentHandler;
    private readonly DeleteStudentHandler _deleteStudentHandler;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<StudentsController> _logger;

    public StudentsController(
        GetStudentsHandler getStudentsHandler,
        GetStudentLearningProgressHandler getStudentLearningProgressHandler,
        CreateStudentHandler createStudentHandler,
        UpdateStudentHandler updateStudentHandler,
        DeleteStudentHandler deleteStudentHandler,
        IUserRepository userRepository,
        ILogger<StudentsController> logger)
    {
        _getStudentsHandler = getStudentsHandler;
        _getStudentLearningProgressHandler = getStudentLearningProgressHandler;
        _createStudentHandler = createStudentHandler;
        _updateStudentHandler = updateStudentHandler;
        _deleteStudentHandler = deleteStudentHandler;
        _userRepository = userRepository;
        _logger = logger;
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
            var currentUserId = GetCurrentUserId();
            var response = await _getStudentsHandler.Handle(request, currentUserId, cancellationToken);
            return Ok(new { success = true, data = response });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get student by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(StudentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStudentById(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
            if (currentUser == null)
                return Forbid();

            var student = await _userRepository.GetByIdAsync(id, cancellationToken);
            if (student == null || student.Role?.Name != RoleNames.Student)
                return NotFound(new { message = "Student not found." });

            if (student.SchoolId != currentUser.SchoolId)
                return NotFound(new { message = "Student not found." });

            _logger.LogInformation("GetStudentById - StudentId: {Id}, IsActive: {IsActive}, Address: {Address}, Gender: {Gender}",
                student.Id, student.IsActive, student.Address, student.Gender);

            return Ok(new { success = true, data = new StudentResponse
            {
                Id = student.Id,
                FullName = student.FullName,
                Email = student.Email,
                Phone = student.Phone,
                Avatar = student.Avatar,
                Gender = student.Gender,
                DateOfBirth = student.DateOfBirth,
                Address = student.Address,
                IsActive = student.IsActive,
                IsEmailVerified = student.IsEmailVerified,
                SchoolId = student.SchoolId,
                SchoolName = student.School?.Name,
                TotalEnrolledClasses = 0,
                CertificatesEarned = 0,
                AverageScore = null,
                CreatedAt = student.CreatedAt,
                UpdatedAt = student.UpdatedAt
            }});
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out int userId))
            return userId;

        throw new UnauthorizedAccessException("User is not authenticated properly.");
    }

    /// <summary>
    /// Create a new student.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(StudentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateStudent(
        [FromBody] CreateStudentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var result = await _createStudentHandler.Handle(request, currentUserId, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new { success = true, data = result });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Bulk create students from Excel import.
    /// </summary>
    [HttpPost("bulk")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkCreateStudents(
        [FromBody] BulkCreateStudentsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var results = new List<BulkCreateStudentResult>();

            // Check duplicate emails within the request itself
            var duplicateEmails = request.Students
                .GroupBy(s => s.Email.Trim().ToLower())
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateEmails.Any())
            {
                return BadRequest(new { success = false, message = $"Các email bị trùng trong file: {string.Join(", ", duplicateEmails)}" });
            }

            foreach (var studentRequest in request.Students)
            {
                try
                {
                    var result = await _createStudentHandler.Handle(studentRequest, currentUserId, cancellationToken);
                    results.Add(new BulkCreateStudentResult
                    {
                        Email = studentRequest.Email,
                        FullName = studentRequest.FullName,
                        Success = true,
                        Message = "Created successfully"
                    });
                }
                catch (InvalidOperationException ex)
                {
                    results.Add(new BulkCreateStudentResult
                    {
                        Email = studentRequest.Email,
                        FullName = studentRequest.FullName,
                        Success = false,
                        Message = ex.Message
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new BulkCreateStudentResult
                    {
                        Email = studentRequest.Email,
                        FullName = studentRequest.FullName,
                        Success = false,
                        Message = ex.Message
                    });
                }
            }

            return StatusCode(StatusCodes.Status201Created, new { success = true, data = results });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(StudentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStudent(
        int id,
        [FromBody] UpdateStudentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var updatedStudent = await _updateStudentHandler.Handle(id, request, currentUserId, cancellationToken);
            if (updatedStudent is null)
                return NotFound(new { success = false, message = "Student not found." });

            return Ok(new { success = true, data = updatedStudent, message = "Student updated successfully." });
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
            return BadRequest(new { success = false, message = ex.Message });
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
            var currentUserId = GetCurrentUserId();
            var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
            if (currentUser == null)
                throw new UnauthorizedAccessException("User is not authenticated properly.");

            var student = await _userRepository.GetByIdAsync(studentId, cancellationToken);
            if (student == null)
                return NotFound(new { message = "Student not found." });

            if (student.Role?.Name != "Student")
                return NotFound(new { message = "Student not found." });

            if (currentUser.SchoolId != student.SchoolId)
                return NotFound(new { message = "Student not found in your school." });

            var response = await _getStudentLearningProgressHandler.Handle(studentId, cancellationToken);
            return Ok(new { success = true, data = response });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Soft delete a student.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> DeleteStudent(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var success = await _deleteStudentHandler.Handle(id, currentUserId, cancellationToken);
            if (!success)
                return NotFound(new { success = false, message = "Student not found." });

            return Ok(new { success = true, message = "Student deleted successfully." });
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
            return StatusCode(500, new { success = false, message = "An error occurred while deleting the student.", error = ex.Message });
        }
    }

    /// <summary>
    /// Get schedule for a specific student.
    /// </summary>
    [HttpGet("{studentId:int}/schedule")]
    [ProducesResponseType(typeof(IEnumerable<ScheduleCalendarResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetStudentSchedule(
        int studentId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
            if (currentUser == null)
                throw new UnauthorizedAccessException("User is not authenticated properly.");

            var student = await _userRepository.GetByIdAsync(studentId, cancellationToken);
            if (student == null)
                return NotFound(new { message = "Student not found." });

            if (student.Role?.Name != "Student")
                return NotFound(new { message = "Student not found." });

            if (currentUser.SchoolId != student.SchoolId)
                return NotFound(new { message = "Student not found in your school." });

            var schedules = await _userRepository.GetStudentSchedulesAsync(studentId, fromDate, toDate, cancellationToken);

            var scheduleColors = new[]
            {
                "#3b82f6", "#10b981", "#f59e0b", "#ef4444", "#8b5cf6",
                "#ec4899", "#06b6d4", "#84cc16", "#f97316", "#6366f1"
            };

            var response = schedules.Select((s, index) => new ScheduleCalendarResponse
            {
                Id = s.Id,
                Title = $"{s.Class?.Course?.Title ?? "Lớp học"} - {s.Class?.ClassCode}",
                Start = s.StartTime,
                End = s.EndTime,
                ClassCode = s.Class?.ClassCode ?? string.Empty,
                ClassName = s.Class?.Course?.Title ?? string.Empty,
                Color = scheduleColors[index % scheduleColors.Length]
            });

            return Ok(new { success = true, data = response });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
