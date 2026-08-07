using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STEM.Core.Entities.Schools;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Api.Controllers;

[ApiController]
[Route("api/school-requests")]
[Authorize(Policy = "MasterOnly")]
public class SchoolRequestsController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IRepository<School> _schoolRepository;

    public SchoolRequestsController(
        IUserRepository userRepository,
        IRepository<School> schoolRepository)
    {
        _userRepository = userRepository;
        _schoolRepository = schoolRepository;
    }

    /// <summary>
    /// Lấy danh sách đơn đăng ký trường đang chờ duyệt.
    /// </summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingRequests(CancellationToken cancellationToken = default)
    {
        try
        {
            var pendingSchools = await _schoolRepository.FindAsync(
                s => s.Status == SchoolStatus.Pending,
                cancellationToken);

            var representativeEmails = pendingSchools.Select(s => s.RepresentativeEmail).Distinct().ToList();
            var users = await _userRepository.FindAsync(
                u => representativeEmails.Contains(u.Email),
                cancellationToken);
            var userMap = users.GroupBy(u => u.Email).ToDictionary(g => g.Key, g => g.First());

            var result = pendingSchools.Select(s => {
                userMap.TryGetValue(s.RepresentativeEmail, out var adminUser);
                return new
                {
                    s.Id,
                    s.Name,
                    s.Address,
                    s.RepresentativeName,
                    s.RepresentativeEmail,
                    s.ProofOfActivity,
                    s.CreatedAt,
                    s.StudentScale,
                    s.RepresentativePosition,
                    s.Website,
                    s.Notes,
                    AdminUser = adminUser != null ? new
                    {
                        adminUser.Id,
                        adminUser.Email,
                        adminUser.FullName,
                        adminUser.Phone
                    } : null
                };
            }).OrderByDescending(s => s.CreatedAt);

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Duyệt đơn đăng ký trường (cả School và User đại diện).
    /// </summary>
    [HttpPost("{schoolId}/approve")]
    public async Task<IActionResult> ApproveRequest(int schoolId, CancellationToken cancellationToken = default)
    {
        try
        {
            var school = await _schoolRepository.GetByIdAsync(schoolId, cancellationToken);
            if (school == null)
                return NotFound(new { success = false, message = "School not found." });

            if (school.Status != SchoolStatus.Pending)
                return BadRequest(new { success = false, message = "School is not pending approval." });

            school.Status = SchoolStatus.Approved;
            school.UpdatedAt = DateTime.UtcNow;
            _schoolRepository.Update(school);

            var adminUser = (await _userRepository.FindAsync(
                u => u.SchoolId == schoolId && u.RoleId == 2,
                cancellationToken)).FirstOrDefault();

            if (adminUser != null)
            {
                adminUser.IsActive = true;
                adminUser.UpdatedAt = DateTime.UtcNow;
                _userRepository.Update(adminUser);
            }

            await _schoolRepository.SaveChangesAsync(cancellationToken);

            return Ok(new { success = true, message = "School and admin user approved." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Từ chối đơn đăng ký trường (xóa cả School và User).
    /// </summary>
    [HttpPost("{schoolId}/reject")]
    public async Task<IActionResult> RejectRequest(int schoolId, CancellationToken cancellationToken = default)
    {
        try
        {
            var school = await _schoolRepository.GetByIdAsync(schoolId, cancellationToken);
            if (school == null)
                return NotFound(new { success = false, message = "School not found." });

            var adminUsers = await _userRepository.FindAsync(
                u => u.SchoolId == schoolId && u.RoleId == 2,
                cancellationToken);

            foreach (var user in adminUsers)
            {
                _userRepository.Delete(user);
            }

            _schoolRepository.Delete(school);
            await _schoolRepository.SaveChangesAsync(cancellationToken);

            return Ok(new { success = true, message = "School request rejected and deleted." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
