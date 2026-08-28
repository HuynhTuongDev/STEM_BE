using STEM.Core.Entities.Curriculum;

namespace STEM.Core.Interfaces;

public interface ISyllabusRepository
{
    Task<Syllabus?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Syllabus?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Syllabus>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Syllabus>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Syllabus>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
    Task<IEnumerable<Syllabus>> GetByGradeLevelAsync(int gradeLevelId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Syllabus>> GetByGradeLevelWithDetailsAsync(int gradeLevelId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Syllabus>> GetPublishedAsync(CancellationToken cancellationToken = default);
    Task<Syllabus> AddAsync(Syllabus syllabus, CancellationToken cancellationToken = default);
    Task<Syllabus> UpdateAsync(Syllabus syllabus, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<int> GetCourseCountAsync(int syllabusId, CancellationToken cancellationToken = default);
    Task<int> GetTotalModulesAsync(int syllabusId, CancellationToken cancellationToken = default);
    Task<int> GetTotalLessonsAsync(int syllabusId, CancellationToken cancellationToken = default);
    Task<bool> PublishAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ArchiveAsync(int id, CancellationToken cancellationToken = default);

    // Extension methods for use cases
    Task<(IEnumerable<Syllabus> Syllabuses, int TotalCount)> GetSyllabusesPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm,
        int? gradeLevelId,
        string? status,
        CancellationToken cancellationToken = default);
    Task<Syllabus?> GetDetailAsync(int id, CancellationToken cancellationToken = default);
    Task<Syllabus?> GetStructureAsync(int id, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
