using STEM.Core.Entities.Courses;

namespace STEM.Core.Interfaces;

public interface IModuleRepository
{
    Task<Module?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Module?> GetByIdWithLessonsAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Module>> GetByCourseIdAsync(int courseId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Module>> GetByCourseIdOrderedAsync(int courseId, CancellationToken cancellationToken = default);
    Task<Module> AddAsync(Module module, CancellationToken cancellationToken = default);
    Task<Module> UpdateAsync(Module module, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<int> GetLessonCountAsync(int moduleId, CancellationToken cancellationToken = default);
    Task UpdateOrdersAsync(int courseId, List<(int ModuleId, int NewOrder)> orders, CancellationToken cancellationToken = default);
}
