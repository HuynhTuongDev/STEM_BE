namespace STEM.Application.Dtos.Curriculum;

public class SyllabusDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int? GradeLevelId { get; set; }
    public string? GradeLevelName { get; set; }
    public string SubjectArea { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public int EstimatedHours { get; set; }
    public bool IsRequired { get; set; }
    public bool IsSystemOwned { get; set; }
    public int CourseCount { get; set; }
    public int TotalModules { get; set; }
    public int TotalLessons { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class SyllabusDetailDto : SyllabusDto
{
    public List<CourseInSyllabusDto> Courses { get; set; } = new();
}

public class CourseInSyllabusDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public int EstimatedHours { get; set; }
    public bool IsRequired { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<ModuleInCourseDto> Modules { get; set; } = new();
}

public class ModuleInCourseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public int EstimatedMinutes { get; set; }
    
    // === INPUT & OUTPUT ===
    public string Input { get; set; } = string.Empty;
    public string Output { get; set; } = string.Empty;
    // ======================
    
    public List<LessonInModuleDto> Lessons { get; set; } = new();
}

public class LessonInModuleDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public int EstimatedMinutes { get; set; }
    public string LessonType { get; set; } = string.Empty;

    // === INPUT & OUTPUT ===
    public string Input { get; set; } = string.Empty;
    public string Output { get; set; } = string.Empty;
    // ======================

    public bool HasVirtualLab { get; set; }
    public Guid? LabId { get; set; }
    public string? LabTitle { get; set; }
}

public class CreateSyllabusRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int? GradeLevelId { get; set; }
    public string SubjectArea { get; set; } = string.Empty;
    public int DisplayOrder { get; set; } = 0;
    public int EstimatedHours { get; set; } = 0;
    public bool IsRequired { get; set; } = true;
}

public class UpdateSyllabusRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int? GradeLevelId { get; set; }
    public string SubjectArea { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public int EstimatedHours { get; set; }
    public bool IsRequired { get; set; }
}

public class PublishSyllabusRequest
{
    public int SyllabusId { get; set; }
}

public class ArchiveSyllabusRequest
{
    public int SyllabusId { get; set; }
}
