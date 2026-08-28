using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Courses;
using STEM.Core.Entities.Schools;
using STEM.Core.Entities.Users;
using STEM.Core.Entities.Projects;
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
            .Include(c => c.GradeLevel)
            .Include(c => c.Course)
            .Include(c => c.Teacher)
            .Where(c => c.CourseId == courseId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Class>> GetByTeacherIdAsync(int teacherId, CancellationToken cancellationToken = default)
    {
        return await GetClassesByTeacherIdAsync(teacherId, cancellationToken);
    }

    public async Task<IEnumerable<Enrollment>> GetStudentEnrollmentsAsync(int studentId, CancellationToken cancellationToken = default)
    {
        // Use projection to avoid cycle with Enrollments
        var enrollments = await _context.Enrollments
            .AsNoTracking()
            .Where(e => e.StudentId == studentId)
            .Select(e => new Enrollment
            {
                Id = e.Id,
                StudentId = e.StudentId,
                ClassId = e.ClassId,
                EnrolledAt = e.EnrolledAt,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt,
                Class = e.Class == null ? null : new Class
                {
                    Id = e.Class.Id,
                    ClassCode = e.Class.ClassCode,
                    SchoolId = e.Class.SchoolId,
                    CourseId = e.Class.CourseId,
                    TeacherId = e.Class.TeacherId,
                    StartDate = e.Class.StartDate,
                    EndDate = e.Class.EndDate,
                    CreatedAt = e.Class.CreatedAt,
                    UpdatedAt = e.Class.UpdatedAt,
                    School = e.Class.School == null ? null : new School
                    {
                        Id = e.Class.School.Id,
                        Name = e.Class.School.Name
                    },
                    Course = e.Class.Course == null ? null : new Course
                    {
                        Id = e.Class.Course.Id,
                        Title = e.Class.Course.Title
                    },
                    Teacher = e.Class.Teacher == null ? null : new User
                    {
                        Id = e.Class.Teacher.Id,
                        FullName = e.Class.Teacher.FullName
                    },
                    Enrollments = new List<Enrollment>(), // Empty to avoid cycle
                    Schedules = e.Class.Schedules == null ? new List<Schedule>() : e.Class.Schedules
                        .Select(s => new Schedule
                        {
                            Id = s.Id,
                            ClassId = s.ClassId,
                            StartTime = s.StartTime,
                            EndTime = s.EndTime
                        }).ToList()
                }
            })
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        return enrollments;
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
            .Include(c => c.GradeLevel)
            .Include(c => c.Course)
            .Include(c => c.Teacher)
            .Include(c => c.Enrollments)
            .AsQueryable();

        if (schoolId.HasValue)
        {
            var schoolIdValue = schoolId.Value;
            query = query.Where(c => c.SchoolId == schoolIdValue);
        }

        if (courseId.HasValue)
        {
            var courseIdValue = courseId.Value;
            query = query.Where(c => c.CourseId == courseIdValue);
        }

        if (teacherId.HasValue)
        {
            var teacherIdValue = teacherId.Value;
            query = query.Where(c => c.TeacherId == teacherIdValue);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(c => c.ClassCode.ToLower().Contains(term) ||
                                     (c.Course != null && c.Course.Title.ToLower().Contains(term)) ||
                                     (c.Teacher != null && c.Teacher.FullName.ToLower().Contains(term)));
        }

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
            .Include(c => c.GradeLevel)
            .Include(c => c.Course)
                .ThenInclude(co => co.Syllabus)
            .Include(c => c.Teacher)
            .Include(c => c.Enrollments).ThenInclude(e => e.Student)
            .Include(c => c.Schedules).ThenInclude(s => s.Lesson)
            .Include(c => c.Announcements)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Schedule>> GetSchedulesByTeacherAsync(int teacherId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        // Convert dates to UTC if they have Kind=Unspecified
        var fromDateUtc = fromDate.HasValue ? DateTime.SpecifyKind(fromDate.Value, DateTimeKind.Utc) : (DateTime?)null;
        var toDateUtc = toDate.HasValue ? DateTime.SpecifyKind(toDate.Value, DateTimeKind.Utc) : (DateTime?)null;

        var query = _context.Schedules
            .Include(s => s.Class)
                .ThenInclude(c => c.Course)
            .Include(s => s.Lesson)
            .Where(s => s.Class.TeacherId == teacherId)
            .AsQueryable();

        if (fromDateUtc.HasValue)
        {
            var fromDateValue = fromDateUtc.Value;
            query = query.Where(s => s.StartTime >= fromDateValue);
        }

        if (toDateUtc.HasValue)
        {
            var toDateValue = toDateUtc.Value;
            query = query.Where(s => s.EndTime <= toDateValue);
        }

        return await query
            .OrderBy(s => s.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Schedule>> GetSchedulesAsync(int classId, CancellationToken cancellationToken = default)
    {
        return await _context.Schedules
            .Include(s => s.Lesson)
            .Where(s => s.ClassId == classId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<int>> GetAvailableTeacherIdsForClassAsync(int classId, CancellationToken cancellationToken = default)
    {
        var targetClass = await _context.Classes
            .Where(c => c.Id == classId)
            .FirstOrDefaultAsync(cancellationToken);

        if (targetClass == null)
            return new List<int>();

        var targetSchedules = await _context.Schedules
            .Where(s => s.ClassId == classId)
            .ToListAsync(cancellationToken);

        var teacherRoleId = 3; // Teacher role ID

        var allTeachers = await _context.Users
            .Where(u => u.SchoolId == targetClass.SchoolId && u.RoleId == teacherRoleId && u.IsActive)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        if (!targetSchedules.Any())
            return allTeachers;

        // Get all classes with their schedules where teacher is assigned
        var teacherClasses = await _context.Classes
            .Include(c => c.Schedules)
            .Where(c => c.TeacherId > 0 && allTeachers.Contains(c.TeacherId))
            .ToListAsync(cancellationToken);

        var availableTeachers = new List<int>();

        foreach (var teacherId in allTeachers)
        {
            var classesWithTeacher = teacherClasses.Where(c => c.TeacherId == teacherId).ToList();
            bool hasConflict = false;

            foreach (var target in targetSchedules)
            {
                foreach (var cls in classesWithTeacher)
                {
                    foreach (var schedule in cls.Schedules)
                    {
                        if (schedule.StartTime < target.EndTime && schedule.EndTime > target.StartTime)
                        {
                            hasConflict = true;
                            break;
                        }
                    }
                    if (hasConflict) break;
                }
                if (hasConflict) break;
            }

            if (!hasConflict)
                availableTeachers.Add(teacherId);
        }

        return availableTeachers;
    }

    public async Task<IEnumerable<Assignment>> GetClassAssignmentsAsync(int classId, CancellationToken cancellationToken = default)
    {
        return await _context.Assignments
            .AsNoTracking()
            .Where(a => a.ClassId == classId)
            .OrderByDescending(a => a.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Course?> GetCourseByIdAsync(int courseId, CancellationToken cancellationToken = default)
    {
        return await _context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId, cancellationToken);
    }

}
