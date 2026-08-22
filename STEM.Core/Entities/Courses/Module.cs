namespace STEM.Core.Entities.Courses;

public class Module : BaseEntity
{
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public int EstimatedMinutes { get; set; }
    public string Input { get; set; } = string.Empty;
    public string Output { get; set; } = string.Empty;

    public Course? Course { get; set; }
}
