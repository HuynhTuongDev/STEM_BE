using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Classes;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Repositories;

public class EnrollmentRepository : Repository<Enrollment>, IEnrollmentRepository
{
    public EnrollmentRepository(StemDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Enrollment>> GetByStudentIdAsync(int studentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(e => e.Class)
                .ThenInclude(c => c!.Course)
            .Where(e => e.StudentId == studentId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Enrollment>> GetByClassIdAsync(int classId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(e => e.Student)
            .Where(e => e.ClassId == classId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<StudentScheduleConflict>> GetConflictingStudentsAsync(int classId, CancellationToken cancellationToken = default)
    {
        // Get all schedules of the target class
        var targetSchedules = await _context.Schedules
            .Where(s => s.ClassId == classId)
            .ToListAsync(cancellationToken);

        if (!targetSchedules.Any())
            return Enumerable.Empty<StudentScheduleConflict>();

        // Get student role ID
        var studentRoleId = await _context.Roles
            .Where(r => r.Name == "Student")
            .Select(r => r.Id)
            .FirstOrDefaultAsync(cancellationToken);

        // Get students enrolled in this class
        var enrolledStudentIds = await _dbSet
            .Where(e => e.ClassId == classId)
            .Select(e => e.StudentId)
            .ToListAsync(cancellationToken);

        // Get all students not in this class with their enrollments and schedules
        var studentSchedules = await (
            from e in _dbSet
            join s in _context.Schedules on e.ClassId equals s.ClassId
            join u in _context.Users on e.StudentId equals u.Id
            where !enrolledStudentIds.Contains(e.StudentId) && u.RoleId == studentRoleId
            select new {
                StudentId = e.StudentId,
                StudentName = u.FullName,
                StudentEmail = u.Email,
                ClassId = e.ClassId,
                StartTime = s.StartTime,
                EndTime = s.EndTime
            }
        ).ToListAsync(cancellationToken);

        var classCodes = await _context.Classes
            .Where(c => studentSchedules.Select(s => s.ClassId).Distinct().Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.ClassCode, cancellationToken);

        // Find conflicts
        var conflicts = studentSchedules
            .Where(ss => targetSchedules.Any(ts => ss.StartTime < ts.EndTime && ss.EndTime > ts.StartTime))
            .GroupBy(ss => ss.StudentId)
            .Select(g => g.First())
            .Select(ss => new StudentScheduleConflict {
                StudentId = ss.StudentId,
                StudentName = ss.StudentName,
                StudentEmail = ss.StudentEmail,
                ConflictingClassId = ss.ClassId,
                ConflictingClassCode = classCodes.GetValueOrDefault(ss.ClassId, ""),
                ConflictingStartTime = ss.StartTime,
                ConflictingEndTime = ss.EndTime
            })
            .ToList();

        return conflicts;
    }

    public async Task<List<int>> GetConflictingStudentIdsAsync(int classId, CancellationToken cancellationToken = default)
    {
        var targetSchedules = await _context.Schedules
            .Where(s => s.ClassId == classId)
            .ToListAsync(cancellationToken);

        if (!targetSchedules.Any())
            return new List<int>();

        // Get students already enrolled in this class
        var enrolledStudentIds = await _dbSet
            .Where(e => e.ClassId == classId)
            .Select(e => e.StudentId)
            .ToListAsync(cancellationToken);

        // Get student role ID
        var studentRoleId = await _context.Roles
            .Where(r => r.Name == "Student")
            .Select(r => r.Id)
            .FirstOrDefaultAsync(cancellationToken);

        // Get all enrollments for students not in this class
        var otherEnrollments = await _dbSet
            .Where(e => !enrolledStudentIds.Contains(e.StudentId))
            .Select(e => e.ClassId)
            .ToListAsync(cancellationToken);

        if (!otherEnrollments.Any())
            return new List<int>();

        // Get all schedules for those enrollments
        var otherSchedules = await _context.Schedules
            .Where(s => otherEnrollments.Contains(s.ClassId))
            .ToListAsync(cancellationToken);

        // Get student IDs for these enrollments
        var studentClassMap = await _dbSet
            .Where(e => !enrolledStudentIds.Contains(e.StudentId))
            .Select(e => new { e.StudentId, e.ClassId })
            .ToListAsync(cancellationToken);

        var studentIds = new HashSet<int>();

        foreach (var studentClass in studentClassMap)
        {
            var schedules = otherSchedules.Where(s => s.ClassId == studentClass.ClassId);
            foreach (var target in targetSchedules)
            {
                foreach (var schedule in schedules)
                {
                    if (schedule.StartTime < target.EndTime && schedule.EndTime > target.StartTime)
                    {
                        studentIds.Add(studentClass.StudentId);
                        break;
                    }
                }
                if (studentIds.Contains(studentClass.StudentId))
                    break;
            }
        }

        return studentIds.ToList();
    }

    public async Task<bool> CanAddStudentToClassAsync(int studentId, int classId, CancellationToken cancellationToken = default)
    {
        // Get schedules of the target class
        var targetSchedules = await _context.Schedules
            .Where(s => s.ClassId == classId)
            .ToListAsync(cancellationToken);

        if (!targetSchedules.Any())
            return true;

        var studentEnrollments = await _dbSet
            .Where(e => e.StudentId == studentId && e.ClassId != classId)
            .Select(e => e.ClassId)
            .ToListAsync(cancellationToken);

        if (!studentEnrollments.Any())
            return true;

        var studentSchedules = await _context.Schedules
            .Where(s => studentEnrollments.Contains(s.ClassId))
            .ToListAsync(cancellationToken);

        foreach (var target in targetSchedules)
        {
            foreach (var student in studentSchedules)
            {
                if (student.StartTime < target.EndTime && student.EndTime > target.StartTime)
                    return false;
            }
        }

        return true;
    }

    public async Task<StudentCourseEnrollment?> GetExistingCourseEnrollmentAsync(
        int studentId,
        int courseId,
        int excludeClassId,
        CancellationToken cancellationToken = default)
    {
        // Tìm enrollment của student vào class cùng course (trừ class hiện tại)
        var enrollment = await _dbSet
            .Include(e => e.Class)
                .ThenInclude(c => c!.Course)
            .Include(e => e.Student)
            .Where(e => e.StudentId == studentId &&
                       e.ClassId != excludeClassId &&
                       e.Class != null &&
                       e.Class.CourseId == courseId)
            .Select(e => new StudentCourseEnrollment
            {
                StudentId = e.StudentId,
                StudentName = e.Student != null ? e.Student.FullName : string.Empty,
                ClassId = e.ClassId,
                ClassCode = e.Class!.ClassCode,
                CourseId = e.Class!.CourseId,
                CourseName = e.Class!.Course != null ? e.Class.Course.Title : string.Empty
            })
            .FirstOrDefaultAsync(cancellationToken);

        return enrollment;
    }
}
