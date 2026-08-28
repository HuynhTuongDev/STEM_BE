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

    public new async Task<Module?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public new async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Module>> GetByCourseIdAsync(int courseId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(m => m.CourseId == courseId)
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<Module?> GetByIdWithLessonsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(m => m.Lessons.OrderBy(l => l.DisplayOrder))
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Module>> GetByCourseIdOrderedAsync(int courseId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(m => m.CourseId == courseId)
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public new async Task<Module> AddAsync(Module module, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(module, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return module;
    }

    public async Task<Module> UpdateAsync(Module module, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(module);
        await _context.SaveChangesAsync(cancellationToken);
        return module;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var module = await _dbSet
            .Include(m => m.Lessons)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (module == null)
            return false;

        _dbSet.Remove(module);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> GetLessonCountAsync(int moduleId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(m => m.Id == moduleId)
            .SelectMany(m => m.Lessons)
            .CountAsync(cancellationToken);
    }

    public async Task UpdateOrdersAsync(int courseId, List<(int ModuleId, int NewOrder)> orders, CancellationToken cancellationToken = default)
    {
        var modules = await _dbSet
            .Where(m => m.CourseId == courseId)
            .ToListAsync(cancellationToken);

        foreach (var (moduleId, newOrder) in orders)
        {
            var module = modules.FirstOrDefault(m => m.Id == moduleId);
            if (module != null)
                module.DisplayOrder = newOrder;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
