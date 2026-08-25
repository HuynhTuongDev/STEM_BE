using STEM.Core.Entities.Curriculum;

namespace STEM.Core.Interfaces;

public interface IGradeLevelRepository
{
    Task<GradeLevel?> GetByIdAsync(int id);
    Task<GradeLevel?> GetByCodeAsync(string code);
    Task<IEnumerable<GradeLevel>> GetAllAsync();
    Task<IEnumerable<GradeLevel>> GetAllOrderedAsync();
    Task<GradeLevel> AddAsync(GradeLevel gradeLevel);
    Task<GradeLevel> UpdateAsync(GradeLevel gradeLevel);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<bool> ExistsByCodeAsync(string code, int? excludeId = null);
    Task<int> GetSyllabusCountAsync(int gradeLevelId);
    Task<int> GetCourseCountAsync(int gradeLevelId);
}
