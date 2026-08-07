using STEM.Core.Entities.Simulations;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Repositories;

public class SimulationSessionRepository : Repository<SimulationSession>, ISimulationSessionRepository
{
    public SimulationSessionRepository(StemDbContext context) : base(context)
    {
    }
}
