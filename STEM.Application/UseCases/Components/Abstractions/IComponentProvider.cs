namespace STEM.Application.UseCases.Components.Abstractions;

// Component Provider (acquisition side) — never called from the runtime/Run
// path (VirtualLabRuntimeService/ISimulationRunnerResolver). Only used from
// admin search/import endpoints and background sync. See ComponentsController.
public interface IComponentProvider
{
    string ProviderName { get; }

    ComponentProviderCapabilities Capabilities { get; }

    Task<IReadOnlyCollection<ExternalComponentCandidate>> SearchAsync(
        string query,
        CancellationToken cancellationToken);

    Task<ExternalComponentCandidate?> GetAsync(
        string externalId,
        CancellationToken cancellationToken);
}

// A provider declares what kinds of data it can actually supply — never
// assume every provider offers everything (Simulation is intentionally
// almost never true for an acquisition-side provider; it belongs to
// ISimulationRunner, not here).
public sealed record ComponentProviderCapabilities(
    bool Metadata,
    bool Visual,
    bool Pins,
    bool Datasheet,
    bool Simulation);

public sealed record ExternalComponentCandidate(
    string Provider,
    string ExternalId,
    string Name,
    string? Category,
    string SourceUrl,
    string? License,
    string? ExternalVersion,
    string? Checksum,
    IReadOnlyCollection<ExternalPinCandidate> Pins,
    string? PrimaryAssetUrl);

// Visual/connector-level pin only — never a simulation identity. See
// FritzingPartParser for where connectorNpin/connectorNterminal get reduced
// to just {VisualPinId, Name}.
public sealed record ExternalPinCandidate(string VisualPinId, string Name);
