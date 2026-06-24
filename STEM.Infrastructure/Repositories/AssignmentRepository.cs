using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Projects;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Repositories;

public class AssignmentRepository : Repository<Assignment>, IAssignmentRepository
{
    public AssignmentRepository(StemDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Assignment>> GetByCourseIdAsync(
        int courseId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(a => a.Class).ThenInclude(c => c!.Course)
            .Where(a => a.Class != null && a.Class.CourseId == courseId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Assignment?> GetByIdWithDetailsAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(a => a.Class).ThenInclude(c => c!.Course)
            .Include(a => a.Class).ThenInclude(c => c!.Teacher)
            .Include(a => a.Class).ThenInclude(c => c!.Enrollments)
            .Include(a => a.Submissions).ThenInclude(s => s.File)
            .Include(a => a.Submissions).ThenInclude(s => s.Student)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<(IEnumerable<Assignment> Assignments, int TotalCount)> GetStudentAssignmentsPagedAsync(
        int studentId,
        int pageNumber,
        int pageSize,
        int? classId,
        string? searchTerm,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(a => a.Class).ThenInclude(c => c!.Course)
            .Include(a => a.Class).ThenInclude(c => c!.Teacher)
            .Include(a => a.Class).ThenInclude(c => c!.Enrollments)
            .Include(a => a.Submissions).ThenInclude(s => s.File)
            .Where(a => a.Class != null &&
                        a.Class.Enrollments.Any(e => e.StudentId == studentId));

        if (classId.HasValue)
        {
            query = query.Where(a => a.ClassId == classId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(a =>
                a.Title.ToLower().Contains(term) ||
                (a.Class != null && a.Class.ClassCode.ToLower().Contains(term)) ||
                (a.Class != null && a.Class.Course != null && a.Class.Course.Title.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var assignments = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (assignments, totalCount);
    }
}
