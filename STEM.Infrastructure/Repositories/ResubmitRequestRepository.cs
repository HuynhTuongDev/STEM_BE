using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Projects;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Repositories;

public class ResubmitRequestRepository : Repository<ResubmitRequest>, IResubmitRequestRepository
{
    public ResubmitRequestRepository(StemDbContext context) : base(context)
    {
    }

    private IQueryable<ResubmitRequest> BuildDetailsQuery()
    {
        return _dbSet
            .Include(request => request.Assignment)
                .ThenInclude(assignment => assignment!.Class)
            .Include(request => request.Student)
            .Include(request => request.ReviewedBy)
            .AsQueryable();
    }

    public async Task<ResubmitRequest?> GetPendingAsync(
        int assignmentId,
        int studentId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(request =>
                request.AssignmentId == assignmentId &&
                request.StudentId == studentId &&
                request.Status == ResubmitRequestStatuses.Pending)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ResubmitRequest?> GetByIdWithDetailsAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await BuildDetailsQuery()
            .FirstOrDefaultAsync(request => request.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<ResubmitRequest>> GetFilteredAsync(
        int? assignmentId,
        string? status,
        int? studentId,
        int? teacherId,
        int? schoolId,
        CancellationToken cancellationToken = default)
    {
        var query = BuildDetailsQuery();

        if (assignmentId.HasValue)
        {
            var assignmentIdValue = assignmentId.Value;
            query = query.Where(request => request.AssignmentId == assignmentIdValue);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(request => request.Status == status);
        }

        if (studentId.HasValue)
        {
            var studentIdValue = studentId.Value;
            query = query.Where(request => request.StudentId == studentIdValue);
        }

        if (teacherId.HasValue)
        {
            var teacherIdValue = teacherId.Value;
            query = query.Where(request =>
                request.Assignment != null &&
                request.Assignment.Class != null &&
                request.Assignment.Class.TeacherId == teacherIdValue);
        }

        if (schoolId.HasValue)
        {
            var schoolIdValue = schoolId.Value;
            query = query.Where(request =>
                request.Assignment != null &&
                request.Assignment.Class != null &&
                request.Assignment.Class.SchoolId == schoolIdValue);
        }

        return await query
            .OrderByDescending(request => request.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
