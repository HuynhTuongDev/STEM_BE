namespace STEM.Core.Entities.Simulations;

public class SimulationTemplate : BaseEntity
{
    public int SimulationId { get; set; }
    public string Config { get; set; } = string.Empty;

    public SimulationEntity? Simulation { get; set; }
}
