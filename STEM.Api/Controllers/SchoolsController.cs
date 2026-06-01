using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STEM.Application.Dtos.Schools;
using STEM.Application.UseCases.Schools;
using STEM.Core.Entities.Schools;
using STEM.Core.Repository;

namespace STEM.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class SchoolsController : ControllerBase
{
    private readonly IRepository<School> _schoolRepository;
    private readonly RegisterSchoolHandler _registerSchoolHandler;

    public SchoolsController(
        IRepository<School> schoolRepository,
        RegisterSchoolHandler registerSchoolHandler)
    {
        _schoolRepository = schoolRepository;
        _registerSchoolHandler = registerSchoolHandler;
    }

    /// <summary>
    /// Đăng ký trường mới (tạo School + User đại diện).
    /// Cần Master Admin duyệt.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> RegisterSchool(
        [FromBody] SchoolRegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _registerSchoolHandler.Handle(request, cancellationToken);
            return Ok(new { success = true, message = "School registration submitted. Pending Master Admin approval." });
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
    /// </summary>
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
    /// </summary>
    [HttpGet("{id}")]
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
}
