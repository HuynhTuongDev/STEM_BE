namespace STEM.Core.Entities.Simulations;

public class SimulationTemplate : BaseEntity
{
    public int SimulationId { get; set; }
    public string SimulationName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Config { get; set; } = string.Empty;

    public SimulationEntity? Simulation { get; set; }
    public ICollection<SimulationSession> SimulationSessions { get; set; } = new List<SimulationSession>();
}
