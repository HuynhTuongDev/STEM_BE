namespace STEM.Application.Dtos.Courses;

public class GetCoursesRequest
{
    public string? SearchTerm { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class CourseListItemResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? SyllabusId { get; set; }
    public string? SyllabusTitle { get; set; }
    public int? EnrolledStudents { get; set; }
    public int EstimatedHours { get; set; }
    public bool IsRequired { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class PagedCourseListResponse
{
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public List<CourseListItemResponse> Items { get; set; } = new();
}

public class CourseDetailResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? SyllabusId { get; set; }
    public string? SyllabusTitle { get; set; }
    public int EstimatedHours { get; set; }
    public bool IsRequired { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateCourseRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? SyllabusId { get; set; }
    public int EstimatedHours { get; set; }
    public bool IsRequired { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

public class UpdateCourseRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? SyllabusId { get; set; }
    public int EstimatedHours { get; set; }
    public bool IsRequired { get; set; }
    public bool IsActive { get; set; }
}
