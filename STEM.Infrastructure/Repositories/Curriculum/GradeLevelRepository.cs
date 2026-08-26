using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Curriculum;
using STEM.Core.Interfaces;

namespace STEM.Infrastructure.Repositories;

public class GradeLevelRepository : Repository<GradeLevel>, IGradeLevelRepository
{
    public GradeLevelRepository(StemDbContext context) : base(context)
    {
    }

    public async Task<GradeLevel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IEnumerable<GradeLevel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(g => g.Id == id, cancellationToken);
    }

    public async Task<GradeLevel?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(g => g.Code == code, cancellationToken);
    }

    public async Task<IEnumerable<GradeLevel>> GetAllOrderedAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .OrderBy(g => g.Level)
            .ToListAsync(cancellationToken);
    }

    public async Task<GradeLevel> AddAsync(GradeLevel gradeLevel, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(gradeLevel, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return gradeLevel;
    }

    public async Task<GradeLevel> UpdateAsync(GradeLevel gradeLevel, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(gradeLevel);
        await _context.SaveChangesAsync(cancellationToken);
        return gradeLevel;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var gradeLevel = await _dbSet.FindAsync(new object[] { id }, cancellationToken);
        if (gradeLevel == null)
            return false;

        _dbSet.Remove(gradeLevel);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ExistsByCodeAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(g => g.Code == code);
        if (excludeId.HasValue)
            query = query.Where(g => g.Id != excludeId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<int> GetSyllabusCountAsync(int gradeLevelId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(g => g.Id == gradeLevelId)
            .SelectMany(g => g.Syllabi)
            .CountAsync(cancellationToken);
    }

    public async Task<int> GetCourseCountAsync(int gradeLevelId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(g => g.Id == gradeLevelId)
            .SelectMany(g => g.Syllabi)
            .SelectMany(s => s.Courses)
            .CountAsync(cancellationToken);
    }
}
