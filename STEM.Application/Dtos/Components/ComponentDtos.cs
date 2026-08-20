namespace STEM.Application.Dtos.Components;

public class ExternalComponentSearchResponse
{
    public string Provider { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string SourceUrl { get; set; } = string.Empty;
    public string? License { get; set; }
    public int PinCount { get; set; }

    // Preview only — computed the SAME way ImportAsync would (Normalizer +
    // SimulationTypeResolver), but nothing is written to the registry here.
    // Null = this candidate would import as NotMapped; the admin sees this
    // BEFORE spending an import action on a component that can't run yet.
    public string? SimulationTypeCandidate { get; set; }
}

public class ImportComponentRequest
{
    public string Provider { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
}

public class ComponentPinResponse
{
    public string VisualPinId { get; set; } = string.Empty;
    public string LogicalPinId { get; set; } = string.Empty;
    public IReadOnlyCollection<string> Aliases { get; set; } = Array.Empty<string>();
}

public class ComponentSourceResponse
{
    public string Provider { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public string? License { get; set; }
    public string LicenseStatus { get; set; } = string.Empty;
    public string? ExternalVersion { get; set; }
    public DateTime ImportedAt { get; set; }
}

public class ComponentDefinitionResponse
{
    public Guid Id { get; set; }
    public string CanonicalKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string Status { get; set; } = string.Empty;

    // Null = NotMapped — frontend/picker must treat this as "visual only",
    // not simulation-ready (STEP 29).
    public string? SimulationComponentType { get; set; }
    public bool SimulationReady => !string.IsNullOrWhiteSpace(SimulationComponentType);

    // Computed from SimulationComponentType via RuntimeCapabilityResolver —
    // never stored, never provider-derived. A component with
    // SimulationComponentType == null (e.g. every RGB LED variant) always
    // resolves to an empty array here, by construction — the frontend never
    // needs its own "is this really interactive" guess.
    public IReadOnlyCollection<string> RuntimeCapabilities { get; set; } = Array.Empty<string>();
    public string? SensorKind { get; set; }

    public IReadOnlyCollection<ComponentPinResponse> Pins { get; set; } = Array.Empty<ComponentPinResponse>();
    public IReadOnlyCollection<ComponentSourceResponse> Sources { get; set; } = Array.Empty<ComponentSourceResponse>();
}
