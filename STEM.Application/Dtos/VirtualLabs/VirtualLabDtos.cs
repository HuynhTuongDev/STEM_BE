namespace STEM.Application.Dtos.VirtualLabs;

public class GetVirtualLabsRequest
{
    public string? SearchTerm { get; set; }
    public int? ClassId { get; set; }
    public int? CourseId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class CreateVirtualLabRequest
{
    public int ClassId { get; set; }
    public string SimulationName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DiagramJson { get; set; } = string.Empty;
}

public class UpdateVirtualLabRequest
{
    public int ClassId { get; set; }
    public string SimulationName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DiagramJson { get; set; } = string.Empty;
}

public class VirtualLabResponse
{
    public int Id { get; set; }
    public int SimulationId { get; set; }
    public int ClassId { get; set; }
    public string ClassCode { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public int TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public int SchoolId { get; set; }
    public string SchoolName { get; set; } = string.Empty;
    public string SimulationName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DiagramJson { get; set; } = string.Empty;
    public int SessionsCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class PagedVirtualLabResponse
{
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public IReadOnlyCollection<VirtualLabResponse> Items { get; set; } = Array.Empty<VirtualLabResponse>();
}
