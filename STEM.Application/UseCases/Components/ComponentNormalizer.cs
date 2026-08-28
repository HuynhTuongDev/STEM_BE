using STEM.Application.UseCases.Components.Abstractions;

namespace STEM.Application.UseCases.Components;

public sealed class ComponentNormalizer : IComponentNormalizer
{
    public NormalizedComponent Normalize(ExternalComponentCandidate candidate)
    {
        var pins = candidate.Pins
            .Select(pin => new NormalizedPin(
                pin.VisualPinId,
                PinAliasRules.Resolve(pin.Name),
                new[] { pin.Name }))
            .ToList();

        return new NormalizedComponent(
            BuildCanonicalKeyCandidate(candidate.Name, candidate.Category),
            candidate.Name,
            candidate.Category,
            pins);
    }

    // "Red LED - 5mm" + category "LED" -> "led.red-led-5mm". Deliberately a
    // *candidate* — STEP 3 says reuse the existing wokwi-* string as the
    // transitional canonical key for anything already simulation-mapped;
    // ComponentRegistryService is the one place that decides whether to keep
    // this generated candidate or override it with an existing type.
    private static string BuildCanonicalKeyCandidate(string name, string? category)
    {
        var slug = Slugify(name);
        var prefix = string.IsNullOrWhiteSpace(category) ? "component" : Slugify(category);
        return $"{prefix}.{slug}";
    }

    private static string Slugify(string value)
    {
        var lowered = value.Trim().ToLowerInvariant();
        var chars = lowered.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        var collapsed = new string(chars);
        while (collapsed.Contains("--", StringComparison.Ordinal))
        {
            collapsed = collapsed.Replace("--", "-", StringComparison.Ordinal);
        }

        return collapsed.Trim('-');
    }
}
