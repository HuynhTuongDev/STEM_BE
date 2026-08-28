using STEM.Core.Entities.Curriculum;

namespace STEM.Core.Interfaces;

public interface IGradeLevelRepository
{
    Task<GradeLevel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<GradeLevel?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IEnumerable<GradeLevel>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<GradeLevel>> GetAllOrderedAsync(CancellationToken cancellationToken = default);
    Task<GradeLevel> AddAsync(GradeLevel gradeLevel, CancellationToken cancellationToken = default);
    Task<GradeLevel> UpdateAsync(GradeLevel gradeLevel, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCodeAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<int> GetSyllabusCountAsync(int gradeLevelId, CancellationToken cancellationToken = default);
    Task<int> GetCourseCountAsync(int gradeLevelId, CancellationToken cancellationToken = default);
}
