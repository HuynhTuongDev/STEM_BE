using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Curriculum;
using STEM.Core.Interfaces;

namespace STEM.Infrastructure.Repositories;

public class SyllabusRepository : Repository<Syllabus>, ISyllabusRepository
{
    public SyllabusRepository(StemDbContext context) : base(context)
    {
    }

    public new async Task<Syllabus?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<Syllabus?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.GradeLevel)
            .Include(s => s.Courses)
                .ThenInclude(c => c.Modules)
                    .ThenInclude(m => m.Lessons)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public new async Task<IEnumerable<Syllabus>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.GradeLevel)
            .OrderBy(s => s.GradeLevel != null ? s.GradeLevel.Level : 999)
            .ThenBy(s => s.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Syllabus>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.GradeLevel)
            .OrderBy(s => s.GradeLevel != null ? s.GradeLevel.Level : 999)
            .ThenBy(s => s.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Syllabus>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.GradeLevel)
            .Where(s => s.Status == status)
            .OrderBy(s => s.GradeLevel != null ? s.GradeLevel.Level : 999)
            .ThenBy(s => s.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Syllabus>> GetByGradeLevelAsync(int gradeLevelId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.GradeLevelId == gradeLevelId)
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Syllabus>> GetByGradeLevelWithDetailsAsync(int gradeLevelId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.GradeLevel)
            .Include(s => s.Courses)
                .ThenInclude(c => c.Modules)
            .Where(s => s.GradeLevelId == gradeLevelId)
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Syllabus>> GetPublishedAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.GradeLevel)
            .Where(s => s.Status == SyllabusStatuses.Published)
            .OrderBy(s => s.GradeLevel != null ? s.GradeLevel.Level : 999)
            .ThenBy(s => s.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public new async Task<Syllabus> AddAsync(Syllabus syllabus, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(syllabus, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return syllabus;
    }

    public async Task<Syllabus> UpdateAsync(Syllabus syllabus, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(syllabus);
        await _context.SaveChangesAsync(cancellationToken);
        return syllabus;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var syllabus = await _dbSet.FindAsync(new object[] { id }, cancellationToken);
        if (syllabus == null)
            return false;

        _dbSet.Remove(syllabus);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public new async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<int> GetCourseCountAsync(int syllabusId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.Id == syllabusId)
            .SelectMany(s => s.Courses)
            .CountAsync(cancellationToken);
    }

    public async Task<int> GetTotalModulesAsync(int syllabusId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.Id == syllabusId)
            .SelectMany(s => s.Courses)
            .SelectMany(c => c.Modules)
            .CountAsync(cancellationToken);
    }

    public async Task<int> GetTotalLessonsAsync(int syllabusId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.Id == syllabusId)
            .SelectMany(s => s.Courses)
            .SelectMany(c => c.Modules)
            .SelectMany(m => m.Lessons)
            .CountAsync(cancellationToken);
    }

    public async Task<bool> PublishAsync(int id, CancellationToken cancellationToken = default)
    {
        var syllabus = await _dbSet.FindAsync(new object[] { id }, cancellationToken);
        if (syllabus == null)
            return false;

        syllabus.Status = SyllabusStatuses.Published;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ArchiveAsync(int id, CancellationToken cancellationToken = default)
    {
        var syllabus = await _dbSet.FindAsync(new object[] { id }, cancellationToken);
        if (syllabus == null)
            return false;

        syllabus.Status = SyllabusStatuses.Archived;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
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
        {
            var gradeLevelIdValue = gradeLevelId.Value;
            query = query.Where(s => s.GradeLevelId == gradeLevelIdValue);
        }

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

    public new async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
