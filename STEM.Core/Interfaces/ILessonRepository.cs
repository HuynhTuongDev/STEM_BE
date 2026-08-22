using STEM.Core.Entities.Courses;

namespace STEM.Core.Interfaces;

public interface ILessonRepository
{
    Task<Lesson?> GetByIdAsync(int id);
    Task<Lesson?> GetByIdWithLabAsync(int id);
    Task<IEnumerable<Lesson>> GetByModuleIdAsync(int moduleId);
    Task<IEnumerable<Lesson>> GetByModuleIdOrderedAsync(int moduleId);
    Task<IEnumerable<Lesson>> GetByCourseIdAsync(int courseId);
    Task<IEnumerable<Lesson>> GetByClassIdAsync(int classId); // Lessons for a class via module->course->classes
    Task<Lesson> AddAsync(Lesson lesson);
    Task<Lesson> UpdateAsync(Lesson lesson);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task UpdateOrdersAsync(int moduleId, List<(int LessonId, int NewOrder)> orders);
}
