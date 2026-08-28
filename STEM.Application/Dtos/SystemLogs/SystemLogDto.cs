namespace STEM.Application.Dtos.SystemLogs;

public class GetSystemLogsRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Action { get; set; }
    public string? Level { get; set; }
    public int? ActorUserId { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

public class SystemLogItemResponse
{
    public int Id { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public int? ActorUserId { get; set; }
    public string? ActorName { get; set; }
    public string? ActorRole { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PagedSystemLogListResponse
{
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public List<SystemLogItemResponse> Items { get; set; } = new();
}
