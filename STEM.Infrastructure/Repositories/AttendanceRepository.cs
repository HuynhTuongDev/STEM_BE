using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Classes;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Repositories;

public class AttendanceRepository : Repository<AttendanceRecord>, IAttendanceRepository
{
    public AttendanceRepository(StemDbContext context) : base(context)
    {
    }

    public async Task<AttendanceRecord?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.AttendanceRecords
            .Include(record => record.Class)
            .Include(record => record.Student)
            .Include(record => record.MarkedBy)
            .FirstOrDefaultAsync(record => record.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<AttendanceRecord>> GetByClassDateAsync(
        int classId,
        DateOnly attendanceDate,
        int? scheduleId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AttendanceRecords
            .Include(record => record.Student)
            .Where(record => record.ClassId == classId && record.AttendanceDate == attendanceDate);

        if (scheduleId.HasValue)
        {
            var scheduleIdValue = scheduleId.Value;
            query = query.Where(record => record.ScheduleId == scheduleIdValue);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<AttendanceRecord> Records, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        int? classId,
        int? studentId,
        DateOnly? attendanceDate,
        int? schoolId,
        int? teacherId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AttendanceRecords
            .Include(record => record.Class)
            .Include(record => record.Schedule)
            .Include(record => record.Student)
            .Include(record => record.MarkedBy)
            .AsQueryable();

        if (classId.HasValue)
        {
            var classIdValue = classId.Value;
            query = query.Where(record => record.ClassId == classIdValue);
        }

        if (studentId.HasValue)
        {
            var studentIdValue = studentId.Value;
            query = query.Where(record => record.StudentId == studentIdValue);
        }

        if (attendanceDate.HasValue)
        {
            var attendanceDateValue = attendanceDate.Value;
            query = query.Where(record => record.AttendanceDate == attendanceDateValue);
        }

        if (schoolId.HasValue)
        {
            var schoolIdValue = schoolId.Value;
            query = query.Where(record => record.Class != null && record.Class.SchoolId == schoolIdValue);
        }

        if (teacherId.HasValue)
        {
            var teacherIdValue = teacherId.Value;
            query = query.Where(record => record.Class != null && record.Class.TeacherId == teacherIdValue);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var records = await query
            .OrderByDescending(record => record.AttendanceDate)
            .ThenBy(record => record.ClassId)
            .ThenBy(record => record.Student != null ? record.Student.FullName : string.Empty)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (records, totalCount);
    }

    public new async Task AddRangeAsync(IEnumerable<AttendanceRecord> records, CancellationToken cancellationToken = default)
    {
        await _context.AttendanceRecords.AddRangeAsync(records, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteByScheduleIdAsync(int scheduleId, CancellationToken cancellationToken = default)
    {
        var records = await _context.AttendanceRecords
            .Where(r => r.ScheduleId == scheduleId)
            .ToListAsync(cancellationToken);

        _context.AttendanceRecords.RemoveRange(records);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
