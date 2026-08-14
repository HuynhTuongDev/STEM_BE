using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Projects;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Repositories;

public class SubmissionRepository : Repository<Submission>, ISubmissionRepository
{
    public SubmissionRepository(StemDbContext context) : base(context)
    {
    }

    public async Task<Submission?> GetByAssignmentAndStudentAsync(
        int assignmentId,
        int studentId,
        CancellationToken cancellationToken = default)
    {
        return await BuildDetailsQuery()
            .FirstOrDefaultAsync(
                submission => submission.AssignmentId == assignmentId && submission.StudentId == studentId,
                cancellationToken);
    }

    public async Task<int> GetAttemptCountAsync(
        int assignmentId,
        int studentId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .CountAsync(
                submission => submission.AssignmentId == assignmentId && submission.StudentId == studentId,
                cancellationToken);
    }

    public async Task<IEnumerable<Submission>> GetByAssignmentIdAsync(
        int assignmentId,
        CancellationToken cancellationToken = default)
    {
        return await BuildDetailsQuery()
            .Where(submission => submission.AssignmentId == assignmentId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Submission>> GetByStudentIdAsync(
        int studentId,
        CancellationToken cancellationToken = default)
    {
        return await BuildDetailsQuery()
            .Where(submission => submission.StudentId == studentId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Submission?> GetByIdWithDetailsAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await BuildDetailsQuery()
            .FirstOrDefaultAsync(submission => submission.Id == id, cancellationToken);
    }

    public async Task<(IEnumerable<Submission> Submissions, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        int? assignmentId,
        int? classId,
        int? studentId,
        int? schoolId,
        int? teacherId,
        CancellationToken cancellationToken = default)
    {
        var query = BuildDetailsQuery();

        if (assignmentId.HasValue)
        {
            query = query.Where(submission => submission.AssignmentId == assignmentId.Value);
        }

        if (classId.HasValue)
        {
            query = query.Where(submission =>
                submission.Assignment != null && submission.Assignment.ClassId == classId.Value);
        }

        if (studentId.HasValue)
        {
            query = query.Where(submission => submission.StudentId == studentId.Value);
        }

        if (schoolId.HasValue)
        {
            query = query.Where(submission =>
                submission.Assignment != null &&
                submission.Assignment.Class != null &&
                submission.Assignment.Class.SchoolId == schoolId.Value);
        }

        if (teacherId.HasValue)
        {
            query = query.Where(submission =>
                submission.Assignment != null &&
                submission.Assignment.Class != null &&
                submission.Assignment.Class.TeacherId == teacherId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var submissions = await query
            .OrderByDescending(submission => submission.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (submissions, totalCount);
    }

    public async Task<IEnumerable<Submission>> GetByStudentIdPagedAsync(
        int studentId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await BuildDetailsQuery()
            .Where(submission => submission.StudentId == studentId)
            .OrderByDescending(submission => submission.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Submission>> GetGradedByStudentIdAsync(
        int studentId,
        CancellationToken cancellationToken = default)
    {
        return await BuildDetailsQuery()
            .Where(submission =>
                submission.StudentId == studentId &&
                submission.Status == SubmissionStatuses.Graded &&
                submission.FinalScore.HasValue &&
                submission.Assignment != null &&
                (submission.Assignment.AssignmentType == AssignmentTypes.TextReport ||
                 submission.Assignment.AssignmentType == AssignmentTypes.PracticalSimulation))
            .ToListAsync(cancellationToken);
    }

    private IQueryable<Submission> BuildDetailsQuery()
    {
        return _dbSet
            .Include(submission => submission.Assignment)
                .ThenInclude(assignment => assignment!.Class)
                    .ThenInclude(classEntity => classEntity!.Course)
            .Include(submission => submission.Assignment)
                .ThenInclude(assignment => assignment!.Class)
                    .ThenInclude(classEntity => classEntity!.School)
            .Include(submission => submission.Assignment)
                .ThenInclude(assignment => assignment!.Class)
                    .ThenInclude(classEntity => classEntity!.Teacher)
            .Include(submission => submission.Assignment)
                .ThenInclude(assignment => assignment!.Class)
                    .ThenInclude(classEntity => classEntity!.Enrollments)
            .Include(submission => submission.Assignment)
                .ThenInclude(assignment => assignment!.Rubric)
            .Include(submission => submission.Student)
            .Include(submission => submission.File)
            .Include(submission => submission.GradedBy)
            .AsQueryable();
    }
}
