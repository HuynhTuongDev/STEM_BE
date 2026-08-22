using STEM.Core.Entities.Courses;
using STEM.Core.Repository;

namespace STEM.Core.Repository;

public interface ISyllabusRepository : IRepository<Syllabus>
{
    Task<(IEnumerable<Syllabus> Syllabuses, int TotalCount)> GetSyllabusesPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm,
        int? gradeLevelId,
        string? status,
        CancellationToken cancellationToken = default);

    Task<Syllabus?> GetDetailAsync(int id, CancellationToken cancellationToken = default);

    Task<Syllabus?> GetStructureAsync(int id, CancellationToken cancellationToken = default);
}
