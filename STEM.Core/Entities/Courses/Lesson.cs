using STEM.Core.Entities.Simulations;

namespace STEM.Core.Entities.Courses;

public class Lesson : BaseEntity
{
    public int ModuleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public int EstimatedMinutes { get; set; }
    public bool HasVirtualLab { get; set; }
    public string Input { get; set; } = string.Empty;
    public string Output { get; set; } = string.Empty;
    public string LessonType { get; set; } = string.Empty;
    public Guid? LabId { get; set; }

    public Module? Module { get; set; }
    public Lab? Lab { get; set; }
}
