using STEM.Core.Entities.Curriculum;

namespace STEM.Core.Interfaces;

public interface ISyllabusRepository
{
    Task<Syllabus?> GetByIdAsync(int id);
    Task<Syllabus?> GetByIdWithDetailsAsync(int id);
    Task<IEnumerable<Syllabus>> GetAllAsync();
    Task<IEnumerable<Syllabus>> GetAllWithDetailsAsync();
    Task<IEnumerable<Syllabus>> GetByStatusAsync(string status);
    Task<IEnumerable<Syllabus>> GetByGradeLevelAsync(int gradeLevelId);
    Task<IEnumerable<Syllabus>> GetByGradeLevelWithDetailsAsync(int gradeLevelId);
    Task<IEnumerable<Syllabus>> GetPublishedAsync();
    Task<Syllabus> AddAsync(Syllabus syllabus);
    Task<Syllabus> UpdateAsync(Syllabus syllabus);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<int> GetCourseCountAsync(int syllabusId);
    Task<int> GetTotalModulesAsync(int syllabusId);
    Task<int> GetTotalLessonsAsync(int syllabusId);
    Task<bool> PublishAsync(int id);
    Task<bool> ArchiveAsync(int id);
}
