using STEM.Core.Entities.Projects;

namespace STEM.Core.Entities.Assessments;

public class Rubric : BaseEntity
{
    public int AssignmentId { get; set; }
    public string Criteria { get; set; } = string.Empty;
    public int MaxScore { get; set; }

    public Assignment? Assignment { get; set; }
}
