namespace STEM.Application.Dtos.Curriculum;

public class ModuleDto
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // === INPUT & OUTPUT ===
    // Input: Những kiến thức cần có TRƯỚC KHI học chương này
    public string Input { get; set; } = string.Empty;
    
    // Output: Những gì học sinh SẼ ĐẠT ĐƯỢC SAU KHI học xong chương này
    public string Output { get; set; } = string.Empty;
    // ======================
    
    public int DisplayOrder { get; set; }
    public int EstimatedMinutes { get; set; }
    public int LessonCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ModuleDetailDto : ModuleDto
{
    public List<LessonInModuleDto> Lessons { get; set; } = new();
}

public class ModuleWithClassDto : ModuleDto
{
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
}

public class CreateModuleRequest
{
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // === INPUT & OUTPUT ===
    public string Input { get; set; } = string.Empty;
    public string Output { get; set; } = string.Empty;
    // ======================
    
    public int DisplayOrder { get; set; } = 0;
    public int EstimatedMinutes { get; set; } = 0;
}

public class UpdateModuleRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // === INPUT & OUTPUT ===
    public string Input { get; set; } = string.Empty;
    public string Output { get; set; } = string.Empty;
    // ======================
    
    public int DisplayOrder { get; set; }
    public int EstimatedMinutes { get; set; }
}

public class ReorderModulesRequest
{
    public int CourseId { get; set; }
    public List<ModuleOrderItem> Modules { get; set; } = new();
}

public class ModuleOrderItem
{
    public int ModuleId { get; set; }
    public int NewOrder { get; set; }
}
