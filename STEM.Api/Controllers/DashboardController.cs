using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Schools;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;
using System.Security.Claims;

namespace STEM.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IRepository<School> _schoolRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRepository<Core.Entities.Courses.Course> _courseRepository;
    private readonly IClassRepository _classRepository;

    public DashboardController(
        IRepository<School> schoolRepository,
        IUserRepository userRepository,
        IRepository<Core.Entities.Courses.Course> courseRepository,
        IClassRepository classRepository)
    {
        _schoolRepository = schoolRepository;
        _userRepository = userRepository;
        _courseRepository = courseRepository;
        _classRepository = classRepository;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

            var allSchools = (await _schoolRepository.GetAllAsync(cancellationToken)).ToList();
            var allUsers = (await _userRepository.GetAllAsync(cancellationToken)).ToList();
            var allCourses = (await _courseRepository.GetAllAsync(cancellationToken)).ToList();
            var allClasses = (await _classRepository.GetAllAsync(cancellationToken)).ToList();

            var now = DateTime.UtcNow;
            var stats = new Dictionary<string, object>();

            // Master Admin stats (user without SchoolId = Master Admin)
            if (user?.SchoolId == null)
            {
                stats["totalSchools"] = allSchools.Count(s => s.Status == SchoolStatus.Approved);
                stats["pendingSchoolRequests"] = allSchools.Count(s => s.Status == SchoolStatus.Pending);
                stats["lockedSchools"] = allSchools.Count(s => s.Status == SchoolStatus.Rejected);
                stats["totalUsers"] = allUsers.Count();
                stats["totalCourses"] = allCourses.Count();
            }
            // School Admin stats (filter by SchoolId)
            else
            {
                var schoolId = user.SchoolId.Value;

                stats["totalUsers"] = allUsers.Count(u => u.SchoolId == schoolId);
                stats["totalTeachers"] = allUsers.Count(u => u.SchoolId == schoolId && u.RoleId == 3);
                stats["totalStudents"] = allUsers.Count(u => u.SchoolId == schoolId && u.RoleId == 4);

                var schoolClasses = allClasses.Where(c => c.SchoolId == schoolId).ToList();
                stats["totalClasses"] = schoolClasses.Count;
                stats["activeClasses"] = schoolClasses.Count(c => c.StartDate <= now && c.EndDate >= now);
            }

            return Ok(new { success = true, data = stats });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("activity")]
    public async Task<IActionResult> GetRecentActivity([FromQuery] int limit = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

            var activities = new List<object>();
            var isMasterAdmin = user?.SchoolId == null;

            var schools = (await _schoolRepository.GetAllAsync(cancellationToken)).ToList();
            var users = (await _userRepository.GetAllAsync(cancellationToken)).ToList();

            // Filter schools for School Admin
            if (!isMasterAdmin)
            {
                schools = schools.Where(s => s.Id == user!.SchoolId).ToList();
                users = users.Where(u => u.SchoolId == user!.SchoolId).ToList();
            }

            foreach (var school in schools.OrderByDescending(s => s.CreatedAt).Take(limit))
            {
                activities.Add(new
                {
                    id = $"school-{school.Id}",
                    type = "school",
                    title = $"Trường mới: {school.Name}",
                    description = school.Address,
                    timestamp = school.CreatedAt.ToString("o"),
                    user = new { name = school.RepresentativeName }
                });
            }

            foreach (var userItem in users.OrderByDescending(u => u.CreatedAt).Take(limit))
            {
                activities.Add(new
                {
                    id = $"user-{userItem.Id}",
                    type = "user",
                    title = $"Người dùng mới: {userItem.FullName}",
                    description = userItem.Email,
                    timestamp = userItem.CreatedAt.ToString("o"),
                    user = new { name = userItem.FullName }
                });
            }

            var result = activities
                .OrderByDescending(a => {
                    var ts = a.GetType().GetProperty("timestamp")?.GetValue(a)?.ToString();
                    return DateTime.TryParse(ts, out var dt) ? dt : DateTime.MinValue;
                })
                .Take(limit)
                .ToList();

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("nameid")?.Value;
        return int.TryParse(userIdClaim, out var id) ? id : 0;
    }

    [HttpGet("chart")]
    public async Task<IActionResult> GetChartData(CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

            if (user == null)
                return Unauthorized(new { success = false, message = "Không tìm thấy người dùng" });

            var isMasterAdmin = user.SchoolId == null;

            // Fetch all users for role distribution
            var allUsers = (await _userRepository.GetAllAsync(cancellationToken)).ToList();

            var chartData = new Dictionary<string, object>();

            // School Admin: Enrollment trend by month (last 6 months from current month)
            if (user.SchoolId != null)
            {
                var schoolId = user.SchoolId.Value;
                var dbContext = HttpContext.RequestServices.GetService<StemDbContext>();
                if (dbContext != null)
                {
                    var filteredEnrollments = dbContext.Enrollments
                        .Include(e => e.Class)
                        .Where(e => e.Class != null && e.Class.SchoolId == schoolId)
                        .ToList();

                    var now = DateTime.UtcNow;
                    var enrollmentTrend = Enumerable.Range(0, 7)
                        .Select(i =>
                        {
                            var targetMonth = now.AddMonths(-6 + i).Month;
                            var targetYear = now.AddMonths(-6 + i).Year;
                            return new
                            {
                                name = $"T{targetMonth}",
                                students = filteredEnrollments.Count(e => e.EnrolledAt.Month == targetMonth && e.EnrolledAt.Year == targetYear)
                            };
                        })
                        .ToList();

                    chartData["enrollmentTrend"] = enrollmentTrend;
                }
            }

            // Master Admin: Schools growth trend
            var allSchools = (await _schoolRepository.GetAllAsync(cancellationToken))
                .Where(s => s.Status == SchoolStatus.Approved)
                .ToList();

            var currentMonth2 = DateTime.UtcNow.Month;
            var schoolsGrowth = Enumerable.Range(0, 6)
                .Select(i =>
                {
                    var targetMonth = currentMonth2 - 5 + i;
                    var year = DateTime.UtcNow.Year;
                    while (targetMonth < 1) { targetMonth += 12; year--; }
                    while (targetMonth > 12) { targetMonth -= 12; year++; }
                    var monthStart = new DateTime(year, targetMonth, 1);
                    var monthEnd = monthStart.AddMonths(1);
                    return new
                    {
                        name = $"T{targetMonth}",
                        schools = allSchools.Count(s => s.CreatedAt >= monthStart && s.CreatedAt < monthEnd)
                    };
                })
                .ToList();

            chartData["schoolsGrowth"] = schoolsGrowth;

            // User distribution by role - filtered by school for School Admin
            var userDistribution = isMasterAdmin
                ? allUsers.Select(u => new { u.RoleId, Name = u.Role?.Name ?? "Unknown" }).ToList()
                : allUsers.Where(u => u.SchoolId == user!.SchoolId).Select(u => new { u.RoleId, Name = u.Role?.Name ?? "Unknown" }).ToList();

            chartData["usersByRole"] = userDistribution
                .GroupBy(u => u.Name)
                .Select(g => new { name = g.Key, value = g.Count() })
                .ToArray();

            return Ok(new { success = true, data = chartData });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
