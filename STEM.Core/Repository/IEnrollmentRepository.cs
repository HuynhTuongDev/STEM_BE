using STEM.Core.Entities.Classes;

namespace STEM.Core.Repository;

public interface IEnrollmentRepository : IRepository<Enrollment>
{
    Task<IEnumerable<Enrollment>> GetByStudentIdAsync(int studentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Enrollment>> GetByClassIdAsync(int classId, CancellationToken cancellationToken = default);
}