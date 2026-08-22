namespace STEM.Core.Entities.Courses;

public class Lesson : BaseEntity
{
    public int ModuleId { get; set; }
    
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    
    // === INPUT & OUTPUT for Engineering Lessons (Bài học) ===
    // Input: Những gì học sinh cần biết TRƯỚC KHI học bài này
    // Ví dụ: "Kiến thức về mạch điện cơ bản, định luật Ohm"
    public string Input { get; set; } = string.Empty;
    
    // Output: Những gì học sinh sẽ ĐẠT ĐƯỢC SAU KHI học xong bài này
    // Ví dụ: "HS có thể thiết kế được mạch điện đơn giản với LED và điện trở"
    public string Output { get; set; } = string.Empty;
    // ==========================================
    
    // Display order within the module
    public int DisplayOrder { get; set; } = 0;
    
    // Estimated duration in minutes (default 45 minutes)
    public int EstimatedMinutes { get; set; } = 45;
    
    // Lesson type: Theory, Lab, Project, etc.
    public string LessonType { get; set; } = LessonTypes.Theory;
    
    // Lab integration
    public bool HasVirtualLab { get; set; } = false;
    public Guid? LabId { get; set; }
    
    // References
    public Module? Module { get; set; }
}

public static class LessonTypes
{
    public const string Theory = "theory";
    public const string Lab = "lab";
    
    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        Theory,
        Lab
    };
}
