namespace STEM.Application.Dtos.Syllabuses;

public class GetSyllabusesRequest
{
    public string? SearchTerm { get; set; }
    public int? GradeLevelId { get; set; }
    public string? Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class SyllabusListItemResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int? GradeLevelId { get; set; }
    public string? GradeLevelName { get; set; }
    public string SubjectArea { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public int EstimatedHours { get; set; }
    public bool IsRequired { get; set; }
    public bool IsSystemOwned { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PagedSyllabusListResponse
{
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public List<SyllabusListItemResponse> Items { get; set; } = new();
}

public class SyllabusDetailResponse
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
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateSyllabusRequest
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

public class UpdateSyllabusRequest
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
}

public class SyllabusStructureResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<SyllabusStructureCourseNode> Courses { get; set; } = new();
}

public class SyllabusStructureCourseNode
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int? SchoolId { get; set; }
    public int DisplayOrder { get; set; }
    public List<SyllabusStructureModuleNode> Modules { get; set; } = new();
}

public class SyllabusStructureModuleNode
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public List<SyllabusStructureLessonNode> Lessons { get; set; } = new();
}

public class SyllabusStructureLessonNode
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool HasVirtualLab { get; set; }
    public SyllabusStructureLabSummary? Lab { get; set; }
}

public class SyllabusStructureLabSummary
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
