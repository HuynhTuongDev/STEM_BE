using STEM.Core.Entities.Classes;

namespace STEM.Core.Repository;

public interface IAttendanceRepository : IRepository<AttendanceRecord>
{
    Task<AttendanceRecord?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AttendanceRecord>> GetByClassDateAsync(
        int classId,
        DateOnly attendanceDate,
        CancellationToken cancellationToken = default);

    Task<(IEnumerable<AttendanceRecord> Records, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        int? classId,
        int? studentId,
        DateOnly? attendanceDate,
        int? schoolId,
        int? teacherId,
        CancellationToken cancellationToken = default);
}
