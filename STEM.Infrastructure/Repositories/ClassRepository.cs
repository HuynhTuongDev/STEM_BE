using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Courses;
using STEM.Core.Entities.Schools;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Repositories;

public class ClassRepository : Repository<Class>, IClassRepository
{
    public ClassRepository(StemDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Class>> GetByCourseIdAsync(int courseId, CancellationToken cancellationToken = default)
    {
        return await _context.Classes
            .Include(c => c.School)
            .Include(c => c.Course)
            .Include(c => c.Teacher)
            .Where(c => c.CourseId == courseId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Class>> GetByTeacherIdAsync(int teacherId, CancellationToken cancellationToken = default)
    {
        return await GetClassesByTeacherIdAsync(teacherId, cancellationToken);
    }

    public async Task<IEnumerable<Class>> GetClassesByTeacherIdAsync(int teacherId, CancellationToken cancellationToken = default)
    {
        return await _context.Classes
            .AsNoTracking()
            .Where(c => c.TeacherId == teacherId)
            .Select(c => new Class
            {
                Id = c.Id,
                ClassCode = c.ClassCode,
                SchoolId = c.SchoolId,
                CourseId = c.CourseId,
                TeacherId = c.TeacherId,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                School = c.School == null ? null : new School
                {
                    Id = c.School.Id,
                    Name = c.School.Name
                },
                Course = c.Course == null ? null : new Course
                {
                    Id = c.Course.Id,
                    Title = c.Course.Title
                },
                Teacher = c.Teacher == null ? null : new User
                {
                    Id = c.Teacher.Id,
                    FullName = c.Teacher.FullName
                },
                Enrollments = c.Enrollments
                    .Select(e => new Enrollment
                    {
                        Id = e.Id,
                        ClassId = e.ClassId,
                        StudentId = e.StudentId,
                        CreatedAt = e.CreatedAt,
                        UpdatedAt = e.UpdatedAt
                    })
                    .ToList()
            })
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Class?> GetByIdSummaryAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Classes
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new Class
            {
                Id = c.Id,
                ClassCode = c.ClassCode,
                SchoolId = c.SchoolId,
                CourseId = c.CourseId,
                TeacherId = c.TeacherId,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                School = c.School == null ? null : new School
                {
                    Id = c.School.Id,
                    Name = c.School.Name
                },
                Course = c.Course == null ? null : new Course
                {
                    Id = c.Course.Id,
                    Title = c.Course.Title
                },
                Teacher = c.Teacher == null ? null : new User
                {
                    Id = c.Teacher.Id,
                    FullName = c.Teacher.FullName
                }
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(IEnumerable<Class> Classes, int TotalCount)> GetClassesPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm,
        int? courseId,
        int? teacherId,
        int? schoolId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Classes
            .Include(c => c.School)
            .Include(c => c.Course)
            .Include(c => c.Teacher)
            .Include(c => c.Enrollments)
            .AsQueryable();

        if (schoolId.HasValue)
            query = query.Where(c => c.SchoolId == schoolId.Value);

        if (courseId.HasValue)
            query = query.Where(c => c.CourseId == courseId.Value);

        if (teacherId.HasValue)
            query = query.Where(c => c.TeacherId == teacherId.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(c => c.ClassCode.Contains(searchTerm) ||
                                     (c.Course != null && c.Course.Title.Contains(searchTerm)) ||
                                     (c.Teacher != null && c.Teacher.FullName.Contains(searchTerm)));

        var totalCount = await query.CountAsync(cancellationToken);

        var classes = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (classes, totalCount);
    }

    public async Task<Class?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Classes
            .Include(c => c.School)
            .Include(c => c.Course)
            .Include(c => c.Teacher)
            .Include(c => c.Enrollments).ThenInclude(e => e.Student)
            .Include(c => c.Schedules)
            .Include(c => c.Announcements)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Schedule>> GetSchedulesByTeacherAsync(int teacherId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var query = _context.Schedules
            .Include(s => s.Class)
                .ThenInclude(c => c.Course)
            .Where(s => s.Class.TeacherId == teacherId)
            .AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(s => s.StartTime >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(s => s.EndTime <= toDate.Value);

        return await query
            .OrderBy(s => s.StartTime)
            .ToListAsync(cancellationToken);
    }
}
