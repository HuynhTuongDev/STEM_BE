namespace STEM.Core.Entities.Components;

// StemFlow's internal, provider-independent component identity — the
// "acquisition side" of Multi-Provider Component Architecture. Deliberately
// separate from ComponentGlueRegistry (STEM.Core/Entities/Simulations),
// which stays the runtime/simulation-compatibility glue layer keyed by the
// literal wokwi-* type string the diagram JSON and simulation runners
// already use — this entity never replaces it, it feeds it via
// SimulationComponentType.
public class ComponentDefinition
{
    public Guid Id { get; set; }

    // Provider-independent identity, e.g. "led.generic". Transitional: for
    // components already usable in Virtual Lab today, this is set equal to
    // the existing wokwi-* type so no diagram/registry data needs migrating.
    public string CanonicalKey { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }

    // IMPORTED -> NORMALIZED -> PIN_MAPPED -> SIMULATION_MAPPED -> VERIFIED
    // (REJECTED terminal). Never auto-advanced past SIMULATION_MAPPED by
    // import/normalize code — VERIFIED requires an explicit human action.
    public string Status { get; set; } = ComponentDefinitionStatuses.Imported;

    // Null = no simulation behavior mapped yet (NotMapped). When set, must be
    // a key that VirtualLabDiagramService.SupportedPins / at least one
    // ISimulationRunner actually recognizes — resolved by
    // SimulationTypeResolver, never guessed by the normalizer itself.
    public string? SimulationComponentType { get; set; }

    // ComponentPinDefinition[] — {VisualPinId, LogicalPinId, SimulationPinId, Aliases[]}
    public string PinsJson { get; set; } = "[]";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ComponentSource> Sources { get; set; } = new List<ComponentSource>();
}

public static class ComponentDefinitionStatuses
{
    public const string Imported = "IMPORTED";
    public const string Normalized = "NORMALIZED";
    public const string PinMapped = "PIN_MAPPED";
    public const string SimulationMapped = "SIMULATION_MAPPED";
    public const string Verified = "VERIFIED";
    public const string Rejected = "REJECTED";
}
