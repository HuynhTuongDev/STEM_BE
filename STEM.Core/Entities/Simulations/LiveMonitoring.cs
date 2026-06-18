using STEM.Core.Entities.Users;

namespace STEM.Core.Entities.Simulations;

public class LiveMonitoring : BaseEntity
{
    public int SessionId { get; set; }
    public int TeacherId { get; set; }

    public SimulationSession? Session { get; set; }
    public User? Teacher { get; set; }
}
