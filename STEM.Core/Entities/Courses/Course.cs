using STEM.Core.Entities.Schools;
using STEM.Core.Entities.Classes;

namespace STEM.Core.Entities.Courses;

public class Course : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? SchoolId { get; set; }
    public int? SyllabusId { get; set; }
    public int DisplayOrder { get; set; }
    public int EstimatedHours { get; set; }
    public bool IsRequired { get; set; }
    public string Status { get; set; } = string.Empty;
    public string SubjectArea { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public School? School { get; set; }
    public Syllabus? Syllabus { get; set; }
    public ICollection<Class> Classes { get; set; } = new List<Class>();
    public ICollection<Module> Modules { get; set; } = new List<Module>();
}
