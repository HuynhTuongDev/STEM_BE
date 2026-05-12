using STEM.Core.Entities.Classes;

namespace STEM.Core.Entities.Projects;

public class Project : BaseEntity
{
    public int ClassId { get; set; }
    public string Title { get; set; } = string.Empty;

    public Class? Class { get; set; }
    public ICollection<ProjectNumber> ProjectNumbers { get; set; } = new List<ProjectNumber>();
}
