using STEM.Core.Entities.Courses;

namespace STEM.Application.Dtos.Curriculum;

public class LessonDto
{
    public int Id { get; set; }
    public int ModuleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    
    // === INPUT & OUTPUT ===
    // Input: Những gì học sinh cần biết TRƯỚC khi học bài này
    public string Input { get; set; } = string.Empty;
    
    // Output: Những gì học sinh sẽ ĐẠT ĐƯỢC SAU khi học xong bài này
    public string Output { get; set; } = string.Empty;
    // ======================
    
    public int DisplayOrder { get; set; }
    public int EstimatedMinutes { get; set; }
    public string LessonType { get; set; } = string.Empty;
    public bool HasVirtualLab { get; set; }
    public Guid? LabId { get; set; }
    public string? LabTitle { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateLessonRequest
{
    public int ModuleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    
    // === INPUT & OUTPUT ===
    public string Input { get; set; } = string.Empty;
    public string Output { get; set; } = string.Empty;
    // ======================
    
    public int DisplayOrder { get; set; } = 0;
    public int EstimatedMinutes { get; set; } = 45;
    public string LessonType { get; set; } = LessonTypes.Theory;
    public bool HasVirtualLab { get; set; } = false;
    public Guid? LabId { get; set; }
}

public class UpdateLessonRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    
    // === INPUT & OUTPUT ===
    public string Input { get; set; } = string.Empty;
    public string Output { get; set; } = string.Empty;
    // ======================
    
    public int DisplayOrder { get; set; }
    public int EstimatedMinutes { get; set; }
    public string LessonType { get; set; } = string.Empty;
    public bool HasVirtualLab { get; set; }
    public Guid? LabId { get; set; }
}

public class ReorderLessonsRequest
{
    public int ModuleId { get; set; }
    public List<LessonOrderItem> Lessons { get; set; } = new();
}

public class LessonOrderItem
{
    public int LessonId { get; set; }
    public int NewOrder { get; set; }
}
