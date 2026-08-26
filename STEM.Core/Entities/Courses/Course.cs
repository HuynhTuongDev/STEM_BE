using STEM.Core.Entities.Schools;
using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Curriculum;

namespace STEM.Core.Entities.Courses;

public class Course : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Link to source syllabus (optional)
    public int? SyllabusId { get; set; }

    // School that owns this course
    public int? SchoolId { get; set; }

    // Display and order
    public int DisplayOrder { get; set; } = 0;

    // Estimated duration
    public int EstimatedHours { get; set; } = 0;

    // Whether this course is required in the syllabus
    public bool IsRequired { get; set; } = true;

    // Status: Draft, Published, Archived
    public bool IsActive { get; set; } = true;

    // STEAM subject categorization
    public string SubjectArea { get; set; } = SubjectAreas.Engineering;

    // Status: Draft, Published, Archived
    public string Status { get; set; } = SyllabusStatuses.Draft;

    // References
    public Syllabus? Syllabus { get; set; }
    public School? School { get; set; }

    // Child entities
    public ICollection<Class> Classes { get; set; } = new List<Class>();
    public ICollection<Module> Modules { get; set; } = new List<Module>();
}
