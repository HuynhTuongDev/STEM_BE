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
            .Include(assignment => assignment.Class)
                .ThenInclude(classEntity => classEntity!.Course)
            .Where(assignment => assignment.Class != null && assignment.Class.CourseId == courseId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Assignment?> GetByIdWithDetailsAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(assignment => assignment.Class)
                .ThenInclude(classEntity => classEntity!.Course)
            .Include(assignment => assignment.Class)
                .ThenInclude(classEntity => classEntity!.School)
            .Include(assignment => assignment.Class)
                .ThenInclude(classEntity => classEntity!.Teacher)
            .Include(assignment => assignment.Class)
                .ThenInclude(classEntity => classEntity!.Enrollments)
            .Include(assignment => assignment.Submissions)
            .Include(assignment => assignment.Metrics)
            .FirstOrDefaultAsync(assignment => assignment.Id == id, cancellationToken);
    }

    public async Task<(IEnumerable<Assignment> Assignments, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm,
        int? classId,
        int? courseId,
        int? schoolId,
        int? teacherId,
        int? studentId,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(assignment => assignment.Class)
                .ThenInclude(classEntity => classEntity!.Course)
            .Include(assignment => assignment.Class)
                .ThenInclude(classEntity => classEntity!.School)
            .Include(assignment => assignment.Class)
                .ThenInclude(classEntity => classEntity!.Teacher)
            .Include(assignment => assignment.Submissions)
            .Include(assignment => assignment.Metrics)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(assignment => assignment.Title.ToLower().Contains(term));
        }

        if (classId.HasValue)
        {
            query = query.Where(assignment => assignment.ClassId == classId.Value);
        }

        if (courseId.HasValue)
        {
            query = query.Where(assignment => assignment.Class != null && assignment.Class.CourseId == courseId.Value);
        }

        if (schoolId.HasValue)
        {
            query = query.Where(assignment => assignment.Class != null && assignment.Class.SchoolId == schoolId.Value);
        }

        if (teacherId.HasValue)
        {
            query = query.Where(assignment => assignment.Class != null && assignment.Class.TeacherId == teacherId.Value);
        }

        if (studentId.HasValue)
        {
            query = query.Where(assignment =>
                assignment.Class != null &&
                assignment.Class.Enrollments.Any(enrollment => enrollment.StudentId == studentId.Value));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var assignments = await query
            .OrderByDescending(assignment => assignment.CreatedAt)
            .ThenBy(assignment => assignment.Title)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (assignments, totalCount);
    }
}
