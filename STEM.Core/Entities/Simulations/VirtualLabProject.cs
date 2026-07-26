namespace STEM.Core.Entities.Simulations;

public class VirtualLabProject
{
    public Guid Id { get; set; }
    public int? UserId { get; set; }
    public Guid? LabId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Board { get; set; } = "esp32";
    public string Language { get; set; } = "arduino";

    public string CodeContent { get; set; } = string.Empty;
    public string DiagramJson { get; set; } = string.Empty;
    public string LibrariesJson { get; set; } = "{}";
    public string Status { get; set; } = VirtualLabProjectStatuses.Stopped;
    public string SimulationEventsJson { get; set; } = "[]";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public static class VirtualLabProjectStatuses
{
    public const string Running = "running";
    public const string Stopped = "stopped";
    public const string Error = "error";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        Running,
        Stopped,
        Error
    };
}
