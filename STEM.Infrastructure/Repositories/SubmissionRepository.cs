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

    public async Task<IEnumerable<Submission>> GetByAssignmentIdAsync(
        int assignmentId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Assignment)
            .Include(s => s.Student)
            .Include(s => s.File)
            .Where(s => s.AssignmentId == assignmentId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Submission>> GetByStudentIdAsync(
        int studentId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Assignment).ThenInclude(a => a!.Class)
            .Include(s => s.File)
            .Where(s => s.StudentId == studentId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Submission?> GetByAssignmentAndStudentAsync(
        int assignmentId,
        int studentId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Assignment).ThenInclude(a => a!.Class)
            .Include(s => s.File)
            .Include(s => s.Student)
            .FirstOrDefaultAsync(
                s => s.AssignmentId == assignmentId && s.StudentId == studentId,
                cancellationToken);
    }

    public async Task<Submission?> GetByIdWithDetailsAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Assignment).ThenInclude(a => a!.Class).ThenInclude(c => c!.Course)
            .Include(s => s.Assignment).ThenInclude(a => a!.Class).ThenInclude(c => c!.Teacher)
            .Include(s => s.Student)
            .Include(s => s.File)
            .Include(s => s.GradedBy)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }
}
