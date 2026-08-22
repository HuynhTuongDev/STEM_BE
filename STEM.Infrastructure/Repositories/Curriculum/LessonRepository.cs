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

    public async Task<Lesson?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _dbSet.AnyAsync(l => l.Id == id);
    }

    public async Task<IEnumerable<Lesson>> GetByModuleIdAsync(int moduleId)
    {
        return await _dbSet
            .Where(l => l.ModuleId == moduleId)
            .OrderBy(l => l.DisplayOrder)
            .ToListAsync();
    }

    public async Task<Lesson?> GetByIdWithLabAsync(int id)
    {
        return await _dbSet
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<IEnumerable<Lesson>> GetByModuleIdOrderedAsync(int moduleId)
    {
        return await _dbSet
            .Where(l => l.ModuleId == moduleId)
            .OrderBy(l => l.DisplayOrder)
            .ToListAsync();
    }

    public async Task<IEnumerable<Lesson>> GetByCourseIdAsync(int courseId)
    {
        return await _dbSet
            .Where(l => l.Module != null && l.Module.CourseId == courseId)
            .OrderBy(l => l.ModuleId)
            .ThenBy(l => l.DisplayOrder)
            .ToListAsync();
    }

    public async Task<IEnumerable<Lesson>> GetByClassIdAsync(int classId)
    {
        // Get lessons for a class via: Class -> Course -> Modules -> Lessons
        return await _dbSet
            .Where(l => l.Module != null && l.Module.Course != null && l.Module.Course.Classes.Any(c => c.Id == classId))
            .OrderBy(l => l.ModuleId)
            .ThenBy(l => l.DisplayOrder)
            .ToListAsync();
    }

    public new async Task<Lesson> AddAsync(Lesson lesson)
    {
        await _dbSet.AddAsync(lesson);
        await _context.SaveChangesAsync();
        return lesson;
    }

    public new async Task<Lesson> UpdateAsync(Lesson lesson)
    {
        _dbSet.Update(lesson);
        await _context.SaveChangesAsync();
        return lesson;
    }

    public new async Task<bool> DeleteAsync(int id)
    {
        var lesson = await _dbSet.FindAsync(id);
        if (lesson == null)
            return false;

        _dbSet.Remove(lesson);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task UpdateOrdersAsync(int moduleId, List<(int LessonId, int NewOrder)> orders)
    {
        var lessons = await _dbSet
            .Where(l => l.ModuleId == moduleId)
            .ToListAsync();

        foreach (var (lessonId, newOrder) in orders)
        {
            var lesson = lessons.FirstOrDefault(l => l.Id == lessonId);
            if (lesson != null)
                lesson.DisplayOrder = newOrder;
        }

        await _context.SaveChangesAsync();
    }
}
