using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STEM.Application.Dtos.Schools;
using STEM.Application.UseCases.Schools;
using STEM.Core.Entities.Schools;
using STEM.Core.Repository;

namespace STEM.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SchoolsController : ControllerBase
{
    private readonly IRepository<School> _schoolRepository;
    private readonly IUserRepository _userRepository;
    private readonly RegisterSchoolHandler _registerSchoolHandler;
    private readonly UpdateSchoolHandler _updateSchoolHandler;
    private readonly DeleteSchoolHandler _deleteSchoolHandler;

    public SchoolsController(
        IRepository<School> schoolRepository,
        IUserRepository userRepository,
        RegisterSchoolHandler registerSchoolHandler,
        UpdateSchoolHandler updateSchoolHandler,
        DeleteSchoolHandler deleteSchoolHandler)
    {
        _schoolRepository = schoolRepository;
        _userRepository = userRepository;
        _registerSchoolHandler = registerSchoolHandler;
        _updateSchoolHandler = updateSchoolHandler;
        _deleteSchoolHandler = deleteSchoolHandler;
    }

    /// <summary>
    /// Đăng ký trường mới (tạo School + User đại diện).
    /// Cần Master Admin duyệt. Public – không cần đăng nhập.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> RegisterSchool(
        [FromBody] SchoolRegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _registerSchoolHandler.Handle(request, cancellationToken);
            return Ok(new 
            { 
                success = true, 
                message = "School registration submitted. Please check your email to verify your account. After verification, your account will require Master Admin approval." 
            });
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { success = false, errors = ex.Errors.Select(e => e.ErrorMessage) });
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

    /// <summary>
    /// Lấy danh sách tất cả trường (tất cả trạng thái).
    /// Dành cho Master Admin quản lý.
    /// Public – không cần đăng nhập.
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetSchools(CancellationToken cancellationToken = default)
    {
        try
        {
            var schools = await _schoolRepository.FindAsync(
                s => true,
                cancellationToken);

            var representativeEmails = schools.Select(s => s.RepresentativeEmail).Distinct().ToList();
            var users = await _userRepository.FindAsync(
                u => representativeEmails.Contains(u.Email),
                cancellationToken);
            var userPhoneMap = users.GroupBy(u => u.Email).ToDictionary(g => g.Key, g => g.First().Phone);

            var result = schools.Select(s => new
            {
                s.Id,
                s.Name,
                s.Address,
                RepresentativeEmail = s.RepresentativeEmail,
                RepresentativeName = s.RepresentativeName,
                Phone = userPhoneMap.TryGetValue(s.RepresentativeEmail, out var phone) ? phone : null,
                CreatedAt = s.CreatedAt,
                s.Status,
                s.ProofOfActivity,
                s.StudentScale,
                s.RepresentativePosition,
                s.Website,
                s.Notes
            }).OrderBy(s => s.Name);

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy thông tin chi tiết một trường.
    /// Public – không cần đăng nhập.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetSchool(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var school = await _schoolRepository.GetByIdAsync(id, cancellationToken);
            if (school == null)
                return NotFound(new { success = false, message = "School not found." });

            var adminUser = await _userRepository.GetByEmailAsync(school.RepresentativeEmail, cancellationToken);

            return Ok(new { success = true, data = new
            {
                school.Id,
                school.Name,
                school.Address,
                RepresentativeEmail = school.RepresentativeEmail,
                RepresentativeName = school.RepresentativeName,
                Phone = adminUser?.Phone,
                CreatedAt = school.CreatedAt,
                school.Status,
                school.ProofOfActivity,
                school.StudentScale,
                school.RepresentativePosition,
                school.Website,
                school.Notes
            }});
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Cập nhật thông tin một trường.
    /// Chỉ School Administrator mới có quyền truy cập.
    /// </summary>
    [Authorize(Policy = "MasterOnly")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateSchool(
        int id,
        [FromBody] UpdateSchoolRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _updateSchoolHandler.Handle(id, request, cancellationToken);
            return Ok(new { success = true, message = "School updated successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Khóa hoặc mở khóa một trường.
    /// Chỉ Master Administrator mới có quyền truy cập.
    /// </summary>
    [Authorize(Policy = "MasterOnly")]
    [HttpPut("{id:int}/toggle-lock")]
    public async Task<IActionResult> ToggleSchoolLock(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var school = await _schoolRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException($"School with id {id} not found.");

            // Toggle between Approved and Rejected/Locked
            school.Status = school.Status == SchoolStatus.Approved 
                ? SchoolStatus.Rejected 
                : SchoolStatus.Approved;

            _schoolRepository.Update(school);
            await _schoolRepository.SaveChangesAsync(cancellationToken);

            var message = school.Status == SchoolStatus.Approved
                ? "School unlocked successfully."
                : "School locked successfully. All users in this school have been disabled.";

            return Ok(new { success = true, message, status = school.Status });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Xóa một trường khỏi hệ thống.
    /// Chỉ School Administrator mới có quyền truy cập.
    /// </summary>
    [Authorize(Policy = "MasterOnly")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteSchool(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _deleteSchoolHandler.Handle(id, cancellationToken);
            return Ok(new { success = true, message = "School deleted successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
