using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Assessments;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Repositories;

public class RubricRepository : IRubricRepository
{
    private readonly StemDbContext _context;

    public RubricRepository(StemDbContext context)
    {
        _context = context;
    }

    public async Task<Rubric?> GetByAssignmentIdAsync(int assignmentId, CancellationToken cancellationToken = default)
    {
        return await _context.Rubrics
            .FirstOrDefaultAsync(r => r.AssignmentId == assignmentId, cancellationToken);
    }

    public async Task AddAsync(Rubric rubric, CancellationToken cancellationToken = default)
    {
        await _context.Rubrics.AddAsync(rubric, cancellationToken);
    }

    public async Task DeleteByAssignmentIdAsync(int assignmentId, CancellationToken cancellationToken = default)
    {
        var rubrics = await _context.Rubrics
            .Where(r => r.AssignmentId == assignmentId)
            .ToListAsync(cancellationToken);
        _context.Rubrics.RemoveRange(rubrics);
    }
}
