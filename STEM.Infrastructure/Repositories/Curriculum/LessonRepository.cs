using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Courses;
using STEM.Core.Interfaces;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Repositories;

public class LessonRepository : Repository<Lesson>, ILessonRepository
{
    public LessonRepository(StemDbContext context) : base(context)
    {
    }

    public async Task<Lesson?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Lesson>> GetByModuleIdAsync(int moduleId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(l => l.ModuleId == moduleId)
            .OrderBy(l => l.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<Lesson?> GetByIdWithLabAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Lesson>> GetByModuleIdOrderedAsync(int moduleId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(l => l.ModuleId == moduleId)
            .OrderBy(l => l.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Lesson>> GetByCourseIdAsync(int courseId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(l => l.Module != null && l.Module.CourseId == courseId)
            .OrderBy(l => l.ModuleId)
            .ThenBy(l => l.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Lesson>> GetByClassIdAsync(int classId, CancellationToken cancellationToken = default)
    {
        // Get lessons for a class via: Class -> Course -> Modules -> Lessons
        return await _dbSet
            .Where(l => l.Module != null && l.Module.Course != null && l.Module.Course.Classes.Any(c => c.Id == classId))
            .OrderBy(l => l.ModuleId)
            .ThenBy(l => l.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<Lesson> AddAsync(Lesson lesson, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(lesson, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return lesson;
    }

    public async Task<Lesson> UpdateAsync(Lesson lesson, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(lesson);
        await _context.SaveChangesAsync(cancellationToken);
        return lesson;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var lesson = await _dbSet.FindAsync(new object[] { id }, cancellationToken);
        if (lesson == null)
            return false;

        _dbSet.Remove(lesson);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task UpdateOrdersAsync(int moduleId, List<(int LessonId, int NewOrder)> orders, CancellationToken cancellationToken = default)
    {
        var lessons = await _dbSet
            .Where(l => l.ModuleId == moduleId)
            .ToListAsync(cancellationToken);

        foreach (var (lessonId, newOrder) in orders)
        {
            var lesson = lessons.FirstOrDefault(l => l.Id == lessonId);
            if (lesson != null)
                lesson.DisplayOrder = newOrder;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
