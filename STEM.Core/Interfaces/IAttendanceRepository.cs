using STEM.Core.Entities.Classes;

namespace STEM.Core.Repository;

public interface IAttendanceRepository : IRepository<Attendance>
{
    Task<IEnumerable<Attendance>> GetByStudentIdAsync(int studentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Attendance>> GetByClassAndStudentAsync(
        int classId,
        int studentId,
        CancellationToken cancellationToken = default);
}
