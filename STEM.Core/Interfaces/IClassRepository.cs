using STEM.Core.Entities.Classes;

namespace STEM.Core.Repository;

public interface IClassRepository : IRepository<Class>
{
    Task<IEnumerable<Class>> GetByCourseIdAsync(int courseId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Class>> GetByTeacherIdAsync(int teacherId, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Class> Classes, int TotalCount)> GetClassesPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm,
        int? courseId,
        int? teacherId,
        int? schoolId,
        CancellationToken cancellationToken = default);
    Task<Class?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);
}
