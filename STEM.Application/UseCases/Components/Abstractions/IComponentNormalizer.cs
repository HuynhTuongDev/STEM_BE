namespace STEM.Application.UseCases.Components.Abstractions;

// External Provider Format -> StemFlow Unified Component Format. Pure,
// synchronous, no I/O — normalization is a data transform, not a network
// call. Never guesses simulation behavior: SimulationTypeResolver (separate,
// explicit allow-list) decides SimulationComponentType, not this class.
public interface IComponentNormalizer
{
    NormalizedComponent Normalize(ExternalComponentCandidate candidate);
}

public sealed record NormalizedComponent(
    string CanonicalKeyCandidate,
    string Name,
    string? Category,
    IReadOnlyCollection<NormalizedPin> Pins);

// LogicalPinId is the alias-resolved name (e.g. "cathode" -> "C"); Aliases
// carries every raw name variant seen so future providers reporting "GND"/
// "GROUND"/"ANODE" etc. resolve to the same LogicalPinId without editing
// this type.
public sealed record NormalizedPin(
    string VisualPinId,
    string LogicalPinId,
    IReadOnlyCollection<string> Aliases);
