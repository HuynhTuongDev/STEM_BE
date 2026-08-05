using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STEM.Application.Interfaces;
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
    private readonly IEmailService _emailService;

    public SchoolRequestsController(
        IUserRepository userRepository,
        IRepository<School> schoolRepository,
        IEmailService emailService)
    {
        _userRepository = userRepository;
        _schoolRepository = schoolRepository;
        _emailService = emailService;
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
                    s.AttachmentUrl,
                    s.AttachmentFileName,
                    s.OriginalAttachmentFileName,
                    s.RejectionReason,
                    AdminUser = adminUser != null ? new
                    {
                        adminUser.Id,
                        adminUser.Email,
                        adminUser.FullName,
                        adminUser.Phone,
                        adminUser.IsEmailVerified
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
    /// Gửi email thông báo cho người đăng ký.
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

            // Gửi email thông báo duyệt
            if (adminUser != null)
            {
                var body = $@"
                    <h2>STEM - Yêu cầu đăng ký được chấp nhận!</h2>
                    <p>Xin chào {adminUser.FullName},</p>
                    <p>Chúc mừng! Yêu cầu đăng ký trường <strong>""{school.Name}""</strong> của bạn đã được Master Admin chấp thuận.</p>
                    <p>Tài khoản quản trị của bạn đã được kích hoạt. Bây giờ bạn có thể đăng nhập và bắt đầu sử dụng hệ thống STEM.</p>
                    <p>Trân trọng,<br/>Đội ngũ STEM</p>
                ";
                await _emailService.SendEmailAsync(adminUser.Email, "STEM - Đăng ký trường được chấp thuận", body, cancellationToken);
            }

            return Ok(new { success = true, message = "Yêu cầu đăng ký đã được duyệt. Email thông báo đã được gửi." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Từ chối đơn đăng ký trường.
    /// Cập nhật trạng thái school thành Rejected và lưu lý do từ chối.
    /// Gửi email thông báo cho người đăng ký.
    /// </summary>
    [HttpPost("{schoolId}/reject")]
    public async Task<IActionResult> RejectRequest(
        int schoolId,
        [FromBody] RejectSchoolRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
                return BadRequest(new { success = false, message = "Lý do từ chối là bắt buộc." });

            var school = await _schoolRepository.GetByIdAsync(schoolId, cancellationToken);
            if (school == null)
                return NotFound(new { success = false, message = "School not found." });

            var adminUsers = await _userRepository.FindAsync(
                u => u.SchoolId == schoolId && u.RoleId == 2,
                cancellationToken);

            var adminUser = adminUsers.FirstOrDefault();
            var adminEmail = adminUser?.Email ?? school.RepresentativeEmail;
            var adminName = adminUser?.FullName ?? school.RepresentativeName;

            // Lưu lý do từ chối vào school thay vì xóa
            school.Status = SchoolStatus.Rejected;
            school.RejectionReason = request.Reason;
            school.UpdatedAt = DateTime.UtcNow;
            _schoolRepository.Update(school);

            // Xóa user admin (nếu có)
            foreach (var user in adminUsers)
            {
                _userRepository.Delete(user);
            }

            await _schoolRepository.SaveChangesAsync(cancellationToken);

            // Gửi email thông báo từ chối
            var body = $@"
                <h2>STEM - Yêu cầu đăng ký bị từ chối</h2>
                <p>Xin chào {adminName},</p>
                <p>Rất tiếc, yêu cầu đăng ký trường <strong>""{school.Name}""</strong> của bạn đã bị từ chối.</p>
                <p><strong>Lý do từ chối:</strong></p>
                <p style='padding: 10px; background-color: #f5f5f5; border-left: 4px solid #dc3545;'>{request.Reason}</p>
                <p>Nếu bạn có thắc mắc, vui lòng liên hệ với đội ngũ quản trị hệ thống.</p>
                <p>Trân trọng,<br/>Đội ngũ STEM</p>
            ";
            await _emailService.SendEmailAsync(adminEmail, "STEM - Đăng ký trường bị từ chối", body, cancellationToken);

            return Ok(new { success = true, message = "Yêu cầu đăng ký đã bị từ chối. Email thông báo đã được gửi." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}

public class RejectSchoolRequest
{
    public string Reason { get; set; } = string.Empty;
}
