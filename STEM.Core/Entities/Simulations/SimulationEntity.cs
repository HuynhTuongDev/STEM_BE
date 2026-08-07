using STEM.Core.Entities.Classes;

namespace STEM.Core.Entities.Simulations;

public class SimulationEntity : BaseEntity
{
    public int ClassId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DiagramJson { get; set; }

    public Class? Class { get; set; }
    public ICollection<SimulationTemplate> SimulationTemplates { get; set; } = new List<SimulationTemplate>();
}
