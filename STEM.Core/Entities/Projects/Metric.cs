namespace STEM.Core.Entities.Projects;

public class Metric : BaseEntity
{
    public int AssignmentId { get; set; }
    public string Criteria { get; set; } = string.Empty;

    public Assignment? Assignment { get; set; }
}
