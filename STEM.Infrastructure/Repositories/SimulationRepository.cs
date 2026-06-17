using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Simulations;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Repositories;

public class SimulationRepository : Repository<SimulationTemplate>, ISimulationRepository
{
    public SimulationRepository(StemDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SimulationTemplate>> GetByStudentAsync(
        int studentId,
        CancellationToken cancellationToken = default)
    {
        return await _context.SimulationTemplates
            .Include(t => t.SimulationSessions)
            .Where(t => t.SimulationSessions.Any(s => s.StudentId == studentId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
