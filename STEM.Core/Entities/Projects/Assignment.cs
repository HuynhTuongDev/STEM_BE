using STEM.Core.Entities.Classes;

namespace STEM.Core.Entities.Projects;

public class Assignment : BaseEntity
{
    public int ClassId { get; set; }
    public string Title { get; set; } = string.Empty;

    public Class? Class { get; set; }
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
    public ICollection<Metric> Metrics { get; set; } = new List<Metric>();
}
