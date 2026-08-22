using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Courses;
using STEM.Core.Interfaces;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Repositories;

public class ModuleRepository : Repository<Module>, IModuleRepository
{
    public ModuleRepository(StemDbContext context) : base(context)
    {
    }

    public async Task<Module?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _dbSet.AnyAsync(m => m.Id == id);
    }

    public async Task<IEnumerable<Module>> GetByCourseIdAsync(int courseId)
    {
        return await _dbSet
            .Where(m => m.CourseId == courseId)
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync();
    }

    public async Task<Module?> GetByIdWithLessonsAsync(int id)
    {
        return await _dbSet
            .Include(m => m.Lessons.OrderBy(l => l.DisplayOrder))
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<IEnumerable<Module>> GetByCourseIdOrderedAsync(int courseId)
    {
        return await _dbSet
            .Where(m => m.CourseId == courseId)
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync();
    }

    public new async Task<Module> AddAsync(Module module)
    {
        await _dbSet.AddAsync(module);
        await _context.SaveChangesAsync();
        return module;
    }

    public new async Task<Module> UpdateAsync(Module module)
    {
        _dbSet.Update(module);
        await _context.SaveChangesAsync();
        return module;
    }

    public new async Task<bool> DeleteAsync(int id)
    {
        var module = await _dbSet
            .Include(m => m.Lessons)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (module == null)
            return false;

        _dbSet.Remove(module);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetLessonCountAsync(int moduleId)
    {
        return await _dbSet
            .Where(m => m.Id == moduleId)
            .SelectMany(m => m.Lessons)
            .CountAsync();
    }

    public async Task UpdateOrdersAsync(int courseId, List<(int ModuleId, int NewOrder)> orders)
    {
        var modules = await _dbSet
            .Where(m => m.CourseId == courseId)
            .ToListAsync();

        foreach (var (moduleId, newOrder) in orders)
        {
            var module = modules.FirstOrDefault(m => m.Id == moduleId);
            if (module != null)
                module.DisplayOrder = newOrder;
        }

        await _context.SaveChangesAsync();
    }
}
