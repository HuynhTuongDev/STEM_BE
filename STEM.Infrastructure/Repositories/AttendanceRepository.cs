using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Classes;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Repositories;

public class AttendanceRepository : Repository<Attendance>, IAttendanceRepository
{
    public AttendanceRepository(StemDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Attendance>> GetByStudentIdAsync(
        int studentId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(attendance => attendance.Class).ThenInclude(classEntity => classEntity!.Course)
            .Include(attendance => attendance.Class).ThenInclude(classEntity => classEntity!.Teacher)
            .Include(attendance => attendance.Schedule)
            .Where(attendance => attendance.StudentId == studentId)
            .OrderByDescending(attendance => attendance.AttendanceDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Attendance>> GetByClassAndStudentAsync(
        int classId,
        int studentId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(attendance => attendance.Class).ThenInclude(classEntity => classEntity!.Course)
            .Include(attendance => attendance.Class).ThenInclude(classEntity => classEntity!.Teacher)
            .Include(attendance => attendance.Schedule)
            .Where(attendance => attendance.ClassId == classId && attendance.StudentId == studentId)
            .OrderByDescending(attendance => attendance.AttendanceDate)
            .ToListAsync(cancellationToken);
    }
}
