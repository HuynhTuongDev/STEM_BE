using STEM.Core.Entities.Courses;

namespace STEM.Core.Interfaces;

public interface IModuleRepository
{
    Task<Module?> GetByIdAsync(int id);
    Task<Module?> GetByIdWithLessonsAsync(int id);
    Task<IEnumerable<Module>> GetByCourseIdAsync(int courseId);
    Task<IEnumerable<Module>> GetByCourseIdOrderedAsync(int courseId);
    Task<Module> AddAsync(Module module);
    Task<Module> UpdateAsync(Module module);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<int> GetLessonCountAsync(int moduleId);
    Task UpdateOrdersAsync(int courseId, List<(int ModuleId, int NewOrder)> orders);
}
