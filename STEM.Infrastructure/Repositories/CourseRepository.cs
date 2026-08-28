using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Courses;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;
using STEM.Infrastructure.Repositories;

namespace STEM.Infrastructure.Repositories;

public class CourseRepository : Repository<Course>, ICourseRepository
{
    public CourseRepository(StemDbContext context) : base(context)
    {
    }

    public async Task<(IEnumerable<Course> Courses, int TotalCount)> GetCoursesPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(c => c.Syllabus)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(c =>
                c.Title.ToLower().Contains(term) ||
                c.Description.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var courses = await query
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Title)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (courses, totalCount);
    }

    public async Task<Course?> GetCourseDetailAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Syllabus)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByTitleAsync(string title, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(c => c.Title.ToLower() == title.ToLower(), cancellationToken);
    }

    public async Task<bool> HasClassesAsync(int courseId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Classes)
            .Where(c => c.Id == courseId)
            .AnyAsync(c => c.Classes.Any(), cancellationToken);
    }
}
