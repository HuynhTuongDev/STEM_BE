using System.Linq;
using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Classes;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Repositories;

public class ScheduleRepository : Repository<Schedule>, IScheduleRepository
{
    public ScheduleRepository(StemDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Schedule>> GetByClassIdAsync(int classId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Class)
                .ThenInclude(c => c.Course)
            .Where(s => s.ClassId == classId)
            .OrderBy(s => s.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Schedule>> GetByRoomAndTimeAsync(int roomId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.StartTime < endTime && s.EndTime > startTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Schedule>> GetByTeacherAndTimeAsync(int teacherId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.StartTime < endTime && s.EndTime > startTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<StudentScheduleConflict>> GetStudentScheduleConflictsAsync(
        int classId,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        // Single optimized query to find all student conflicts
        // Students are Users with RoleId = 4
        var conflicts = await (
            from e1 in _context.Enrollments
            join e2 in _context.Enrollments on e1.StudentId equals e2.StudentId
            join s in _dbSet on e2.ClassId equals s.ClassId
            join c in _context.Classes on s.ClassId equals c.Id
            join u in _context.Users on e1.StudentId equals u.Id
            where e1.ClassId == classId
               && e2.ClassId != classId
               && u.RoleId == 4 // Student role
               && s.StartTime < endTime
               && s.EndTime > startTime
            select new StudentScheduleConflict
            {
                StudentId = u.Id,
                StudentName = u.FullName,
                StudentEmail = u.Email,
                ConflictingClassId = s.ClassId,
                ConflictingClassCode = c.ClassCode,
                ConflictingStartTime = s.StartTime,
                ConflictingEndTime = s.EndTime
            }
        ).ToListAsync(cancellationToken);

        return conflicts.DistinctBy(c => c.StudentId);
    }

    public async Task<IEnumerable<TeacherScheduleConflict>> GetTeacherScheduleConflictsAsync(
        int classId,
        int teacherId,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        // Single query to find teacher conflicts
        return await (
            from s in _dbSet
            join c in _context.Classes on s.ClassId equals c.Id
            where c.TeacherId == teacherId
               && c.Id != classId
               && s.StartTime < endTime
               && s.EndTime > startTime
            select new TeacherScheduleConflict
            {
                ConflictingClassId = s.ClassId,
                ConflictingClassCode = c.ClassCode,
                ConflictingStartTime = s.StartTime,
                ConflictingEndTime = s.EndTime
            }
        ).ToListAsync(cancellationToken);
    }

    // Combined method - check both student and teacher conflicts in ONE query
    public async Task<(IEnumerable<StudentScheduleConflict> StudentConflicts, IEnumerable<TeacherScheduleConflict> TeacherConflicts)> GetAllConflictsAsync(
        int classId,
        int teacherId,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        // Get student conflicts
        var studentConflicts = await GetStudentScheduleConflictsAsync(classId, startTime, endTime, cancellationToken);

        // Get teacher conflicts
        var teacherConflicts = await GetTeacherScheduleConflictsAsync(classId, teacherId, startTime, endTime, cancellationToken);

        return (studentConflicts, teacherConflicts);
    }

    public new async Task DeleteAsync(Schedule entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
