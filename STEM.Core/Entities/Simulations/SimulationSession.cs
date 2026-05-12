using STEM.Core.Entities.Users;

namespace STEM.Core.Entities.Simulations;

public class SimulationSession : BaseEntity
{
    public int StudentId { get; set; }
    public DateTime StartTime { get; set; }

    public User? Student { get; set; }
    public ICollection<ExperimentLog> ExperimentLogs { get; set; } = new List<ExperimentLog>();
    public ICollection<LiveMonitoring> LiveMonitorings { get; set; } = new List<LiveMonitoring>();
}
