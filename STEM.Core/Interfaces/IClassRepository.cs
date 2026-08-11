using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Projects;

namespace STEM.Core.Repository;

public interface IClassRepository : IRepository<Class>
{
    Task<IEnumerable<Class>> GetByCourseIdAsync(int courseId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Class>> GetByTeacherIdAsync(int teacherId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Enrollment>> GetStudentEnrollmentsAsync(int studentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Assignment>> GetClassAssignmentsAsync(int classId, CancellationToken cancellationToken = default);

    Task<IEnumerable<Class>> GetClassesByTeacherIdAsync(int teacherId, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Class> Classes, int TotalCount)> GetClassesPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm,
        int? courseId,
        int? teacherId,
        int? schoolId,
        CancellationToken cancellationToken = default);
    Task<Class?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Schedule>> GetSchedulesByTeacherAsync(int teacherId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
    Task<Class?> GetByIdSummaryAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Schedule>> GetSchedulesAsync(int classId, CancellationToken cancellationToken = default);
    Task<List<int>> GetAvailableTeacherIdsForClassAsync(int classId, CancellationToken cancellationToken = default);
}
