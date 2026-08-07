namespace STEM.Core.Entities.Simulations;

public class ExperimentLog : BaseEntity
{
    public int SessionId { get; set; }
    public string Log { get; set; } = string.Empty;

    public SimulationSession? Session { get; set; }
}
