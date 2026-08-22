using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Courses;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Repositories;

public class SyllabusRepository : Repository<Syllabus>, ISyllabusRepository
{
    public SyllabusRepository(StemDbContext context) : base(context)
    {
    }

    public async Task<(IEnumerable<Syllabus> Syllabuses, int TotalCount)> GetSyllabusesPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm,
        int? gradeLevelId,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(s => s.GradeLevel)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(s =>
                s.Title.ToLower().Contains(term) ||
                s.Description.ToLower().Contains(term));
        }

        if (gradeLevelId.HasValue)
            query = query.Where(s => s.GradeLevelId == gradeLevelId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(s => s.Status == status);

        var totalCount = await query.CountAsync(cancellationToken);

        var syllabuses = await query
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.Title)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (syllabuses, totalCount);
    }

    public async Task<Syllabus?> GetDetailAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.GradeLevel)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Syllabus?> GetStructureAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.GradeLevel)
            .Include(s => s.Courses.OrderBy(c => c.DisplayOrder))
                .ThenInclude(c => c.Modules.OrderBy(m => m.DisplayOrder))
                    .ThenInclude(m => m.Lessons.OrderBy(l => l.DisplayOrder))
                        .ThenInclude(l => l.Lab)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }
}
