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
    private static readonly IReadOnlyDictionary<string, SimulationTypeRule> CategoryToRule =
        new Dictionary<string, SimulationTypeRule>(StringComparer.OrdinalIgnoreCase)
        {
            ["LED"] = new("wokwi-led", new[] { "A", "C" }),
            ["BUTTON"] = new("wokwi-pushbutton", Array.Empty<string>()),
            ["PUSHBUTTON"] = new("wokwi-pushbutton", Array.Empty<string>()),
            ["BUZZER"] = new("wokwi-buzzer", Array.Empty<string>()),
            ["SERVO"] = new("wokwi-servo", new[] { "GND", "PWM" }),
            ["SERVOMOTOR"] = new("wokwi-servo", new[] { "GND", "PWM" }),
        };

    // Null = NotMapped. Deliberately does not fall back to a fuzzy/best-guess
    // match — an unrecognized category must stay NotMapped, not silently
    // attach fabricated simulation behavior (STEP 8's explicit rule).
    //
    // Category alone is NOT sufficient — found live (2026-08, this phase's
    // STEP 11 verification): a real Fritzing "RGB LED (4 legs)" reports
    // family="LED" (same string as a plain single-color LED) but has 4 pins
    // (red/green/blue cathode + common anode) that don't normalize to
    // wokwi-led's actual "A"/"C" pins at all. Resolving it to "wokwi-led"
    // would have been exactly the false SIMULATION_MAPPED claim STEP 8
    // warns against ("có trong thư viện" != "chạy mô phỏng được"). So this
    // also requires the normalized logical pins to be a superset of the
    // simulation type's minimum required pins when that type declares any
    // (an empty required-pins list, e.g. buzzer/button, has no discriminating
    // pin *names* worth checking here — pin *count*/wiring correctness is
    // VirtualLabDiagramService's job at Analyze() time, not this resolver's).
    public static string? Resolve(string? category, IReadOnlyCollection<string>? normalizedLogicalPinIds = null)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return null;
        }

        if (!CategoryToRule.TryGetValue(category.Trim(), out var rule))
        {
            return null;
        }

        if (rule.RequiredPins.Count == 0)
        {
            return rule.SimulationType;
        }

        var pins = normalizedLogicalPinIds ?? Array.Empty<string>();
        var pinSet = new HashSet<string>(pins, StringComparer.OrdinalIgnoreCase);
        return rule.RequiredPins.All(pinSet.Contains) ? rule.SimulationType : null;
    }

    private sealed record SimulationTypeRule(string SimulationType, IReadOnlyCollection<string> RequiredPins);
}
