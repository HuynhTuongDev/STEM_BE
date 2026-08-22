using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Curriculum;
using STEM.Core.Interfaces;

namespace STEM.Infrastructure.Repositories;

public class SyllabusRepository : Repository<Syllabus>, ISyllabusRepository
{
    public SyllabusRepository(StemDbContext context) : base(context)
    {
    }

    public async Task<Syllabus?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<Syllabus?> GetByIdWithDetailsAsync(int id)
    {
        return await _dbSet
            .Include(s => s.GradeLevel)
            .Include(s => s.Courses)
                .ThenInclude(c => c.Modules)
                    .ThenInclude(m => m.Lessons)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<IEnumerable<Syllabus>> GetAllAsync()
    {
        return await _dbSet
            .Include(s => s.GradeLevel)
            .OrderBy(s => s.GradeLevel != null ? s.GradeLevel.Level : 999)
            .ThenBy(s => s.DisplayOrder)
            .ToListAsync();
    }

    public async Task<IEnumerable<Syllabus>> GetAllWithDetailsAsync()
    {
        return await _dbSet
            .Include(s => s.GradeLevel)
            .OrderBy(s => s.GradeLevel != null ? s.GradeLevel.Level : 999)
            .ThenBy(s => s.DisplayOrder)
            .ToListAsync();
    }

    public async Task<IEnumerable<Syllabus>> GetByStatusAsync(string status)
    {
        return await _dbSet
            .Include(s => s.GradeLevel)
            .Where(s => s.Status == status)
            .OrderBy(s => s.GradeLevel != null ? s.GradeLevel.Level : 999)
            .ThenBy(s => s.DisplayOrder)
            .ToListAsync();
    }

    public async Task<IEnumerable<Syllabus>> GetByGradeLevelAsync(int gradeLevelId)
    {
        return await _dbSet
            .Where(s => s.GradeLevelId == gradeLevelId)
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync();
    }

    public async Task<IEnumerable<Syllabus>> GetByGradeLevelWithDetailsAsync(int gradeLevelId)
    {
        return await _dbSet
            .Include(s => s.GradeLevel)
            .Include(s => s.Courses)
                .ThenInclude(c => c.Modules)
            .Where(s => s.GradeLevelId == gradeLevelId)
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync();
    }

    public async Task<IEnumerable<Syllabus>> GetPublishedAsync()
    {
        return await _dbSet
            .Include(s => s.GradeLevel)
            .Where(s => s.Status == SyllabusStatuses.Published)
            .OrderBy(s => s.GradeLevel != null ? s.GradeLevel.Level : 999)
            .ThenBy(s => s.DisplayOrder)
            .ToListAsync();
    }

    public new async Task<Syllabus> AddAsync(Syllabus syllabus)
    {
        await _dbSet.AddAsync(syllabus);
        await _context.SaveChangesAsync();
        return syllabus;
    }

    public new async Task<Syllabus> UpdateAsync(Syllabus syllabus)
    {
        _dbSet.Update(syllabus);
        await _context.SaveChangesAsync();
        return syllabus;
    }

    public new async Task<bool> DeleteAsync(int id)
    {
        var syllabus = await _dbSet.FindAsync(id);
        if (syllabus == null)
            return false;

        _dbSet.Remove(syllabus);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _dbSet.AnyAsync(s => s.Id == id);
    }

    public async Task<int> GetCourseCountAsync(int syllabusId)
    {
        return await _dbSet
            .Where(s => s.Id == syllabusId)
            .SelectMany(s => s.Courses)
            .CountAsync();
    }

    public async Task<int> GetTotalModulesAsync(int syllabusId)
    {
        return await _dbSet
            .Where(s => s.Id == syllabusId)
            .SelectMany(s => s.Courses)
            .SelectMany(c => c.Modules)
            .CountAsync();
    }

    public async Task<int> GetTotalLessonsAsync(int syllabusId)
    {
        return await _dbSet
            .Where(s => s.Id == syllabusId)
            .SelectMany(s => s.Courses)
            .SelectMany(c => c.Modules)
            .SelectMany(m => m.Lessons)
            .CountAsync();
    }

    public async Task<bool> PublishAsync(int id)
    {
        var syllabus = await _dbSet.FindAsync(id);
        if (syllabus == null)
            return false;

        syllabus.Status = SyllabusStatuses.Published;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ArchiveAsync(int id)
    {
        var syllabus = await _dbSet.FindAsync(id);
        if (syllabus == null)
            return false;

        syllabus.Status = SyllabusStatuses.Archived;
        await _context.SaveChangesAsync();
        return true;
    }
}
