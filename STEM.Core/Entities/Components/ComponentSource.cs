namespace STEM.Core.Entities.Components;

// Provenance record — one ComponentDefinition can have N sources (one per
// provider that contributed to it), never the other way around ("1
// Component = 1 Provider" is explicitly the wrong model).
public class ComponentSource
{
    public Guid Id { get; set; }
    public Guid ComponentId { get; set; }

    public string Provider { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;

    // Never auto-published (see LicenseStatuses.ReviewRequired) when a
    // provider doesn't expose clear license metadata in its own data.
    public string? License { get; set; }
    public string LicenseStatus { get; set; } = ComponentLicenseStatuses.Unknown;

    public string? ExternalVersion { get; set; }
    public string? Checksum { get; set; }

    // Raw external asset URLs (svg/icon/datasheet) as reported by the
    // provider — visual-only, never read by the simulation runtime.
    public string AssetsJson { get; set; } = "[]";

    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastSyncedAt { get; set; }

    public ComponentDefinition? Component { get; set; }
}

public static class ComponentLicenseStatuses
{
    public const string Unknown = "Unknown";
    public const string ReviewRequired = "ReviewRequired";
    public const string Confirmed = "Confirmed";
}
