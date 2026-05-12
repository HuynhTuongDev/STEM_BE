using STEM.Core.Entities.Users;

namespace STEM.Core.Entities.Projects;

public class ProjectNumber : BaseEntity
{
    public int ProjectId { get; set; }
    public int StudentId { get; set; }

    public Project? Project { get; set; }
    public User? Student { get; set; }
}
