namespace STEM.Core.Entities.Courses;

public class Syllabus : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int? GradeLevelId { get; set; }
    public string SubjectArea { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public int EstimatedHours { get; set; }
    public bool IsRequired { get; set; }
    public bool IsSystemOwned { get; set; }

    public GradeLevel? GradeLevel { get; set; }
    public ICollection<Course> Courses { get; set; } = new List<Course>();
}
