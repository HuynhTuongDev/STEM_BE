using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Classes;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Repositories;

public class ClassRepository : Repository<Class>, IClassRepository
{
    public ClassRepository(StemDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Class>> GetByCourseIdAsync(int courseId, CancellationToken cancellationToken = default)
    {
        return await _context.Classes
            .Include(c => c.School)
            .Include(c => c.Course)
            .Include(c => c.Teacher)
            .Where(c => c.CourseId == courseId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Class>> GetByTeacherIdAsync(int teacherId, CancellationToken cancellationToken = default)
    {
        return await _context.Classes
            .Include(c => c.School)
            .Include(c => c.Course)
            .Include(c => c.Teacher)
            .Where(c => c.TeacherId == teacherId)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<Class> Classes, int TotalCount)> GetClassesPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm,
        int? courseId,
        int? teacherId,
        int? schoolId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Classes
            .Include(c => c.School)
            .Include(c => c.Course)
            .Include(c => c.Teacher)
            .Include(c => c.Enrollments)
            .AsQueryable();

        if (schoolId.HasValue)
            query = query.Where(c => c.SchoolId == schoolId.Value);

        if (courseId.HasValue)
            query = query.Where(c => c.CourseId == courseId.Value);

        if (teacherId.HasValue)
            query = query.Where(c => c.TeacherId == teacherId.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(c => c.ClassCode.Contains(searchTerm) ||
                                     (c.Course != null && c.Course.Title.Contains(searchTerm)) ||
                                     (c.Teacher != null && c.Teacher.FullName.Contains(searchTerm)));

        var totalCount = await query.CountAsync(cancellationToken);

        var classes = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (classes, totalCount);
    }

    public async Task<Class?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Classes
            .Include(c => c.School)
            .Include(c => c.Course)
            .Include(c => c.Teacher)
            .Include(c => c.Enrollments).ThenInclude(e => e.Student)
            .Include(c => c.Schedules)
            .Include(c => c.Announcements)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }
}
