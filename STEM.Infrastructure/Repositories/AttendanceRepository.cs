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
        CancellationToken cancellationToken = default)
    {
        return await _context.AttendanceRecords
            .Include(record => record.Student)
            .Where(record => record.ClassId == classId && record.AttendanceDate == attendanceDate)
            .ToListAsync(cancellationToken);
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
            .Include(record => record.Student)
            .Include(record => record.MarkedBy)
            .AsQueryable();

        if (classId.HasValue)
        {
            query = query.Where(record => record.ClassId == classId.Value);
        }

        if (studentId.HasValue)
        {
            query = query.Where(record => record.StudentId == studentId.Value);
        }

        if (attendanceDate.HasValue)
        {
            query = query.Where(record => record.AttendanceDate == attendanceDate.Value);
        }

        if (schoolId.HasValue)
        {
            query = query.Where(record => record.Class != null && record.Class.SchoolId == schoolId.Value);
        }

        if (teacherId.HasValue)
        {
            query = query.Where(record => record.Class != null && record.Class.TeacherId == teacherId.Value);
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
}
