using STEM.Core.Entities.Classes;

namespace STEM.Core.Repository;

public interface IEnrollmentRepository : IRepository<Enrollment>
{
    Task<IEnumerable<Enrollment>> GetByStudentIdAsync(int studentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Enrollment>> GetByClassIdAsync(int classId, CancellationToken cancellationToken = default);
    Task<IEnumerable<StudentScheduleConflict>> GetConflictingStudentsAsync(int classId, CancellationToken cancellationToken = default);
    Task<List<int>> GetConflictingStudentIdsAsync(int classId, CancellationToken cancellationToken = default);
    Task<bool> CanAddStudentToClassAsync(int studentId, int classId, CancellationToken cancellationToken = default);
}