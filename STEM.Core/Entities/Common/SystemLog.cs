namespace STEM.Core.Entities.Common;

/// <summary>
/// Business/security audit trail — distinct from ILogger diagnostics.
/// Append-only by design: does NOT inherit BaseEntity (which forces an
/// UpdatedAt), since an audit record must never look "updatable".
/// </summary>
public class SystemLog
{
    public int Id { get; set; }
    public string Level { get; set; } = SystemLogLevels.Information;
    public string Action { get; set; } = string.Empty;
    public int? ActorUserId { get; set; }
    public string? ActorRole { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public static class SystemLogLevels
{
    public const string Information = "Information";
    public const string Warning = "Warning";
    public const string Critical = "Critical";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        Information,
        Warning,
        Critical
    };
}

/// <summary>
/// Canonical action names. Start with the events that already have a clear,
/// single-purpose handler — do not pre-enumerate every future feature.
/// </summary>
public static class SystemLogActions
{
    public const string SyllabusCreated = "SyllabusCreated";
    public const string SyllabusUpdated = "SyllabusUpdated";
    public const string SyllabusArchived = "SyllabusArchived";

    public const string SchoolApproved = "SchoolApproved";
    public const string SchoolRejected = "SchoolRejected";
}
