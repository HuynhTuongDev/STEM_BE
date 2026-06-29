using STEM.Core.Entities.Projects;

namespace STEM.Core.Repository;

public interface IAssignmentRepository : IRepository<Assignment>
{
    Task<IEnumerable<Assignment>> GetByCourseIdAsync(int courseId, CancellationToken cancellationToken = default);

    Task<Assignment?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);

    Task<(IEnumerable<Assignment> Assignments, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm,
        int? classId,
        int? courseId,
        int? schoolId,
        int? teacherId,
        int? studentId,
        CancellationToken cancellationToken = default);
}
