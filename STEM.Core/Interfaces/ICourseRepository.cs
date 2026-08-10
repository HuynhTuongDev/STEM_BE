using STEM.Core.Entities.Courses;
using STEM.Core.Repository;

namespace STEM.Core.Repository;

public interface ICourseRepository : IRepository<Course>
{
    Task<(IEnumerable<Course> Courses, int TotalCount)> GetCoursesPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm,
        int? schoolId,
        CancellationToken cancellationToken = default);

    Task<Course?> GetCourseDetailAsync(int id, CancellationToken cancellationToken = default);
    
    Task<bool> ExistsByTitleAsync(string title, int schoolId, CancellationToken cancellationToken = default);
    
    Task<bool> HasClassesAsync(int courseId, CancellationToken cancellationToken = default);
}
