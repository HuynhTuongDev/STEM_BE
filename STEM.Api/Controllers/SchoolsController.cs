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
    private readonly RegisterSchoolHandler _registerSchoolHandler;
    private readonly UpdateSchoolHandler _updateSchoolHandler;
    private readonly DeleteSchoolHandler _deleteSchoolHandler;

    public SchoolsController(
        IRepository<School> schoolRepository,
        RegisterSchoolHandler registerSchoolHandler,
        UpdateSchoolHandler updateSchoolHandler,
        DeleteSchoolHandler deleteSchoolHandler)
    {
        _schoolRepository = schoolRepository;
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
    /// Lấy danh sách tất cả trường đã được duyệt.
    /// Public – không cần đăng nhập.
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetSchools(CancellationToken cancellationToken = default)
    {
        try
        {
            var schools = await _schoolRepository.FindAsync(
                s => s.Status == SchoolStatus.Approved,
                cancellationToken);

            var result = schools.Select(s => new
            {
                s.Id,
                s.Name,
                s.Address
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
    /// Chỉ School Administrator mới có quyền truy cập.
    /// </summary>
    [Authorize(Policy = "MasterOnly")]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetSchool(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var school = await _schoolRepository.GetByIdAsync(id, cancellationToken);
            if (school == null)
                return NotFound(new { success = false, message = "School not found." });

            return Ok(new { success = true, data = new
            {
                school.Id,
                school.Name,
                school.Address,
                school.RepresentativeEmail,
                school.RepresentativeName,
                school.Status
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
