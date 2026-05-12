using STEM.Core.Entities.Common;

namespace STEM.Core.Entities.Projects;

public class ProjectMember : BaseEntity
{
    public int ProjectId { get; set; }
    public int UserId { get; set; }
    public string Role { get; set; } = string.Empty; // e.g., Leader, Member

    public Project? Project { get; set; }
    public STEM.Core.Entities.Users.User? User { get; set; }
}
