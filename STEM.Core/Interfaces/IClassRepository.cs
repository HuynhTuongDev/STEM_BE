using STEM.Core.Entities.Classes;

namespace STEM.Core.Repository;

public interface IClassRepository : IRepository<Class>
{
    Task<IEnumerable<Class>> GetByCourseIdAsync(int courseId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Class>> GetByTeacherIdAsync(int teacherId, CancellationToken cancellationToken = default);
}