using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Curriculum;
using STEM.Core.Interfaces;


namespace STEM.Infrastructure.Repositories;

public class GradeLevelRepository : Repository<GradeLevel>, IGradeLevelRepository
{
    public GradeLevelRepository(StemDbContext context) : base(context)
    {
    }

    public async Task<GradeLevel?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<IEnumerable<GradeLevel>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _dbSet.AnyAsync(g => g.Id == id);
    }

    public async Task<GradeLevel?> GetByCodeAsync(string code)
    {
        return await _dbSet.FirstOrDefaultAsync(g => g.Code == code);
    }

    public async Task<IEnumerable<GradeLevel>> GetAllOrderedAsync()
    {
        return await _dbSet
            .OrderBy(g => g.Level)
            .ToListAsync();
    }

    public new async Task<GradeLevel> AddAsync(GradeLevel gradeLevel)
    {
        await _dbSet.AddAsync(gradeLevel);
        await _context.SaveChangesAsync();
        return gradeLevel;
    }

    public new async Task<GradeLevel> UpdateAsync(GradeLevel gradeLevel)
    {
        _dbSet.Update(gradeLevel);
        await _context.SaveChangesAsync();
        return gradeLevel;
    }

    public new async Task<bool> DeleteAsync(int id)
    {
        var gradeLevel = await _dbSet.FindAsync(id);
        if (gradeLevel == null)
            return false;

        _dbSet.Remove(gradeLevel);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsByCodeAsync(string code, int? excludeId = null)
    {
        var query = _dbSet.Where(g => g.Code == code);
        if (excludeId.HasValue)
            query = query.Where(g => g.Id != excludeId.Value);

        return await query.AnyAsync();
    }

    public async Task<int> GetSyllabusCountAsync(int gradeLevelId)
    {
        return await _dbSet
            .Where(g => g.Id == gradeLevelId)
            .SelectMany(g => g.Syllabi)
            .CountAsync();
    }

    public async Task<int> GetCourseCountAsync(int gradeLevelId)
    {
        return await _dbSet
            .Where(g => g.Id == gradeLevelId)
            .SelectMany(g => g.Syllabi)
            .SelectMany(s => s.Courses)
            .CountAsync();
    }
}
