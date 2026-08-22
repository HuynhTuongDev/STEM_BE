using System.Text.Json;
using STEM.Core.Entities.Simulations;

namespace STEM.Application.Dtos.Labs;

public class GetLabsRequest
{
    public string? SearchTerm { get; set; }
    public string? Category { get; set; }
    public int? ClassId { get; set; }
    public string? Status { get; set; }
    public string? SimulationMode { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class CreateLabRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string SimulationMode { get; set; } = "wokwi_iframe";
    public string BoardType { get; set; } = LabBoardTypes.ArduinoUno;
    public string? StarterCode { get; set; }
    public JsonElement CircuitConfig { get; set; }
    public IReadOnlyCollection<string> AllowedComponentTypes { get; set; } = Array.Empty<string>();
    public string? WokwiProjectId { get; set; }
    public string? WokwiProjectUrl { get; set; }
    public IReadOnlyCollection<int> ClassIds { get; set; } = Array.Empty<int>();
    public string Status { get; set; } = "draft";
    public int? LinkedAssignmentId { get; set; }
    public int? ScheduleId { get; set; } // Gán lab vào buổi dạy cụ thể
}

public class UpdateLabRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string SimulationMode { get; set; } = "wokwi_iframe";
    public string BoardType { get; set; } = LabBoardTypes.ArduinoUno;
    public string? StarterCode { get; set; }
    public JsonElement CircuitConfig { get; set; }
    public IReadOnlyCollection<string> AllowedComponentTypes { get; set; } = Array.Empty<string>();
    public string? WokwiProjectId { get; set; }
    public string? WokwiProjectUrl { get; set; }
    public IReadOnlyCollection<int> ClassIds { get; set; } = Array.Empty<int>();
    public string Status { get; set; } = "draft";
    public int? LinkedAssignmentId { get; set; }
    public int? ScheduleId { get; set; } // Gán lab vào buổi dạy cụ thể
}

public class ValidateWokwiProjectRequest
{
    public string? WokwiProjectId { get; set; }
    public string? WokwiProjectUrl { get; set; }
}

public class ValidateWokwiProjectResponse
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? WokwiProjectId { get; set; }
    public string? WokwiProjectUrl { get; set; }
    public int? StatusCode { get; set; }
}

public class LabResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string SimulationMode { get; set; } = string.Empty;
    public string BoardType { get; set; } = string.Empty;
    public string? StarterCode { get; set; }
    public JsonElement CircuitConfig { get; set; }
    public IReadOnlyCollection<string> AllowedComponentTypes { get; set; } = Array.Empty<string>();
    public string? WokwiProjectId { get; set; }
    public string? WokwiProjectUrl { get; set; }
    public string? WokwiEmbedUrl { get; set; }
    public int CreatedById { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public IReadOnlyCollection<LabClassResponse> Classes { get; set; } = Array.Empty<LabClassResponse>();
    public IReadOnlyCollection<int> ClassIds { get; set; } = Array.Empty<int>();
    public string Status { get; set; } = string.Empty;
    public int? LinkedAssignmentId { get; set; }
    public string? LinkedAssignmentTitle { get; set; }
    public LabStatsResponse Stats { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class LabClassResponse
{
    public int Id { get; set; }
    public string ClassCode { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public int SchoolId { get; set; }
    public int TeacherId { get; set; }
}

public class LabProgressResponse
{
    public Guid LabId { get; set; }
    public int StudentId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime LastOpenedAt { get; set; }
    public int OpenCount { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? DurationSeconds { get; set; }
}

public class LabStatsResponse
{
    public Guid LabId { get; set; }
    public int StudentCount { get; set; }
    public int StartedCount { get; set; }
    public int CompletedCount { get; set; }
    public double CompletionRate { get; set; }
    public double? AverageDurationSeconds { get; set; }
    public string CompletionSource { get; set; } = "manual";
}

public class PagedLabResponse
{
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public IReadOnlyCollection<LabResponse> Items { get; set; } = Array.Empty<LabResponse>();
}

public class ComponentGlueRegistryResponse
{
    public string ComponentType { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool Supported { get; set; }
    public JsonElement PinRequirements { get; set; }
    public DateTime UpdatedAt { get; set; }
}
