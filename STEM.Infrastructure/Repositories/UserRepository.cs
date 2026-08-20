using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Courses;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(StemDbContext context) : base(context)
    {
    }

    public override async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.Role)
            .Include(u => u.School)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.Role)
            .Include(u => u.School)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.Role)
            .Include(u => u.School)
            .FirstOrDefaultAsync(u => u.Phone == phone, cancellationToken);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.Role)
            .Include(u => u.School)
            .FirstOrDefaultAsync(u => u.FullName == username, cancellationToken);
    }

    public async Task<(IEnumerable<User> Users, int TotalCount)> GetUsersPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm,
        int? roleId,
        bool? isActive,
        int? schoolId,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(u => u.Role)
            .Include(u => u.School)
            .AsQueryable();

        if (roleId.HasValue)
        {
            query = query.Where(u => u.RoleId == roleId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        if (schoolId.HasValue)
        {
            query = query.Where(u => u.SchoolId == schoolId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(term) 
                                     || u.Email.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (users, totalCount);
    }

    public async Task<IEnumerable<User>> GetStudentsNotInClassAsync(int classId, int schoolId, string? searchTerm, CancellationToken cancellationToken = default)
    {
        var studentRoleName = RoleNames.Student;
        var query = _dbSet
            .Include(u => u.Role)
            .Include(u => u.School)
            .Where(u => !_context.Enrollments.Any(e => e.ClassId == classId && e.StudentId == u.Id))
            .Where(u => u.SchoolId == schoolId)
            .Where(u => u.Role != null && u.Role.Name == studentRoleName)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(term) 
                                     || u.Email.ToLower().Contains(term));
        }

        return await query
            .OrderBy(u => u.FullName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Schedule>> GetStudentSchedulesAsync(int studentId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        // Convert dates to UTC if they have Kind=Unspecified
        var fromDateUtc = fromDate.HasValue ? DateTime.SpecifyKind(fromDate.Value, DateTimeKind.Utc) : (DateTime?)null;
        var toDateUtc = toDate.HasValue ? DateTime.SpecifyKind(toDate.Value, DateTimeKind.Utc) : (DateTime?)null;

        // Include Class and Enrollments for filtering
        var schedules = await _context.Schedules
            .Include(s => s.Class)
                .ThenInclude(c => c!.Enrollments)
            .Where(s => s.Class != null && s.Class.Enrollments != null && s.Class.Enrollments.Any(e => e.StudentId == studentId))
            .Where(s => (!fromDateUtc.HasValue || s.StartTime >= fromDateUtc.Value)
                     && (!toDateUtc.HasValue || s.EndTime <= toDateUtc.Value))
            .Select(s => new Schedule
            {
                Id = s.Id,
                ClassId = s.ClassId,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            })
            .OrderBy(s => s.StartTime)
            .ToListAsync(cancellationToken);

        // Fetch class info separately to avoid cycle
        var classIds = schedules.Select(s => s.ClassId).Distinct().ToList();
        var classInfos = await _context.Classes
            .Where(c => classIds.Contains(c.Id))
            .Select(c => new { c.Id, c.ClassCode, CourseTitle = c.Course != null ? c.Course.Title : "" })
            .ToListAsync(cancellationToken);
        
        var classInfoDict = classInfos.ToDictionary(c => c.Id);

        // Attach class info to schedules
        foreach (var schedule in schedules)
        {
            if (classInfoDict.TryGetValue(schedule.ClassId, out var info))
            {
                schedule.Class = new Class
                {
                    Id = info.Id,
                    ClassCode = info.ClassCode,
                    Course = new Course { Title = info.CourseTitle }
                };
            }
        }

        return schedules;
    }

    public async Task<(IEnumerable<User> Users, int TotalCount)> GetTeachersWithClassCountAsync(int schoolId, int page, int pageSize, string? searchTerm, CancellationToken cancellationToken = default)
    {
        var teacherRoleId = 3; // Teacher

        var query = _dbSet
            .Include(u => u.Role)
            .Include(u => u.School)
            .Where(u => u.SchoolId == schoolId && u.RoleId == teacherRoleId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(term)
                                     || u.Email.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (users, totalCount);
    }

    public async Task<IEnumerable<User>> GetBySchoolIdAsync(int schoolId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.Role)
            .Include(u => u.School)
            .Where(u => u.SchoolId == schoolId)
            .OrderBy(u => u.RoleId)
            .ThenBy(u => u.FullName)
            .ToListAsync(cancellationToken);
    }

    public async Task<(int TeacherCount, int StudentCount)> GetTeacherStudentCountBySchoolAsync(int schoolId, CancellationToken cancellationToken = default)
    {
        var users = await _dbSet
            .Include(u => u.Role)
            .Where(u => u.SchoolId == schoolId)
            .ToListAsync(cancellationToken);

        var teacherCount = users.Count(u => u.RoleId == 3); // RoleId 3 = Teacher
        var studentCount = users.Count(u => u.RoleId == 4); // RoleId 4 = Student

        return (teacherCount, studentCount);
    }
}
