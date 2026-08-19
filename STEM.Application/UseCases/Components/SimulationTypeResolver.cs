namespace STEM.Application.UseCases.Components;

// Canonical Component -> existing simulation component type (STEP 12,
// "Component Resolver tối thiểu"). Deliberately a small, explicit allow-list
// — never a guess. Only maps to wokwi-* types confirmed (2026-08, this
// audit) to actually have simulation behavior in at least one
// ISimulationRunner (VirtualLabMockRunner/EducationalSimulationRunner/
// QemuEsp32Runner), not just "exists in SupportedPins/ComponentGlueRegistry"
// — matching the IMPORTED-vs-SIMULATION_TESTED distinction this whole
// architecture exists to enforce. Extend this table, never the runners
// themselves, when a new category earns real simulation support.
public static class SimulationTypeResolver
{
    private static readonly IReadOnlyDictionary<string, string> CategoryToSimulationType =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["LED"] = "wokwi-led",
            ["BUTTON"] = "wokwi-pushbutton",
            ["PUSHBUTTON"] = "wokwi-pushbutton",
            ["BUZZER"] = "wokwi-buzzer",
            ["SERVO"] = "wokwi-servo",
            ["SERVOMOTOR"] = "wokwi-servo",
        };

    // Null = NotMapped. Deliberately does not fall back to a fuzzy/best-guess
    // match — an unrecognized category must stay NotMapped, not silently
    // attach fabricated simulation behavior (STEP 8's explicit rule).
    public static string? Resolve(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return null;
        }

        return CategoryToSimulationType.TryGetValue(category.Trim(), out var type) ? type : null;
    }
}
