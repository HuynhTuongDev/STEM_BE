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

            // Master Admin stats
            stats["totalSchools"] = allSchools.Count(s => s.Status == SchoolStatus.Approved);
            stats["pendingSchoolRequests"] = allSchools.Count(s => s.Status == SchoolStatus.Pending);
            stats["lockedSchools"] = allSchools.Count(s => s.Status == SchoolStatus.Rejected);
            stats["totalUsers"] = allUsers.Count();
            stats["totalCourses"] = allCourses.Count();

            // School Admin / Teacher / Student stats - filter by SchoolId
            if (user?.SchoolId != null)
            {
                var schoolId = user.SchoolId.Value;

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
            var activities = new List<object>();

            var schools = (await _schoolRepository.GetAllAsync(cancellationToken)).ToList();
            var users = (await _userRepository.GetAllAsync(cancellationToken)).ToList();

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

            foreach (var user in users.OrderByDescending(u => u.CreatedAt).Take(limit))
            {
                activities.Add(new
                {
                    id = $"user-{user.Id}",
                    type = "user",
                    title = $"Người dùng mới: {user.FullName}",
                    description = user.Email,
                    timestamp = user.CreatedAt.ToString("o"),
                    user = new { name = user.FullName }
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

            // DEBUG
            Console.WriteLine($"[DEBUG] UserId: {userId}, SchoolId: {user.SchoolId}, Role: {user.Role?.Name}");

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

            // User distribution by role
            var allUsers = (await _userRepository.GetAllAsync(cancellationToken)).ToList();
            var userDistribution = new[]
            {
                new { name = "Master Admin", value = allUsers.Count(u => u.RoleId == 1) },
                new { name = "School Admin", value = allUsers.Count(u => u.RoleId == 2) },
                new { name = "Giáo viên", value = allUsers.Count(u => u.RoleId == 3) },
                new { name = "Học sinh", value = allUsers.Count(u => u.RoleId == 4) },
            };
            chartData["usersByRole"] = userDistribution;

            return Ok(new { success = true, data = chartData });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
