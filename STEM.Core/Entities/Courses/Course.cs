using STEM.Core.Entities.Schools;
using STEM.Core.Entities.Classes;

namespace STEM.Core.Entities.Courses;

public class Course : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? SchoolId { get; set; }

    public School? School { get; set; }
    public ICollection<Class> Classes { get; set; } = new List<Class>();
    public ICollection<Module> Modules { get; set; } = new List<Module>();
    public ICollection<Material> Materials { get; set; } = new List<Material>();
}
