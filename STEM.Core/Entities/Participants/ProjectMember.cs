using STEM.Core.Entities.Users;
using STEM.Core.Entities.Projects;

namespace STEM.Core.Entities.Participants;

public class ProjectMember : BaseEntity
{
    public int ProjectId { get; set; }
    public int StudentId { get; set; }

    public Project? Project { get; set; }
    public User? Student { get; set; }
}
