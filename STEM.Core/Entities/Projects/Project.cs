using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Participants;

namespace STEM.Core.Entities.Projects;

public class Project : BaseEntity
{
    public int ClassId { get; set; }
    public string Title { get; set; } = string.Empty;

    public Class? Class { get; set; }
    public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
}
