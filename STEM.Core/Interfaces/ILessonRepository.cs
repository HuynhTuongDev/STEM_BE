using STEM.Core.Entities.Courses;

namespace STEM.Core.Interfaces;

public interface ILessonRepository
{
    Task<Lesson?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Lesson?> GetByIdWithLabAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lesson>> GetByModuleIdAsync(int moduleId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lesson>> GetByModuleIdOrderedAsync(int moduleId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lesson>> GetByCourseIdAsync(int courseId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lesson>> GetByClassIdAsync(int classId, CancellationToken cancellationToken = default);
    Task<Lesson> AddAsync(Lesson lesson, CancellationToken cancellationToken = default);
    Task<Lesson> UpdateAsync(Lesson lesson, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task UpdateOrdersAsync(int moduleId, List<(int LessonId, int NewOrder)> orders, CancellationToken cancellationToken = default);
}
