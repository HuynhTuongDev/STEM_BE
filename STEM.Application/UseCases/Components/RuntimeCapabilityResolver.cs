namespace STEM.Application.UseCases.Components;

// Maps an EXISTING canonical simulation type (whatever
// SimulationTypeResolver already resolved to) to the realtime interaction
// capability ISimulationInputChannel/CircuitCanvas already understand
// (Digital/Analog/Sensor/Output — see STEM.Application.UseCases.Simulation.
// Abstractions.SimulationInputType and CircuitCanvas.tsx's interaction
// props). Deliberately keyed by simulationComponentType, not by provider or
// category — a component's capability is a property of what it simulates
// AS, not where it was imported from. A type with no entry here (including
// null/NotMapped) has no runtime interaction capability, which is the
// correct default (STEP 8/9 of this milestone: backend is the sole source
// of capability, never inferred on the frontend from name/category).
public static class RuntimeCapabilityResolver
{
    private static readonly IReadOnlyDictionary<string, RuntimeCapabilityInfo> CapabilitiesByType =
        new Dictionary<string, RuntimeCapabilityInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["wokwi-led"] = new(RuntimeCapabilities.Output, null),
            ["wokwi-buzzer"] = new(RuntimeCapabilities.Output, null),
            ["wokwi-servo"] = new(RuntimeCapabilities.Output, null),
            ["wokwi-pushbutton"] = new(RuntimeCapabilities.DigitalInput, null),
            ["wokwi-potentiometer"] = new(RuntimeCapabilities.AnalogInput, null),
            ["wokwi-photoresistor-sensor"] = new(RuntimeCapabilities.SensorInput, "light"),
            ["wokwi-relay-module"] = new(RuntimeCapabilities.Output, null),
        };

    public static RuntimeCapabilityInfo? Resolve(string? simulationComponentType)
    {
        if (string.IsNullOrWhiteSpace(simulationComponentType))
        {
            return null;
        }

        return CapabilitiesByType.TryGetValue(simulationComponentType, out var info) ? info : null;
    }
}

public sealed record RuntimeCapabilityInfo(string Capability, string? SensorKind);

public static class RuntimeCapabilities
{
    public const string DigitalInput = "DigitalInput";
    public const string AnalogInput = "AnalogInput";
    public const string SensorInput = "SensorInput";
    public const string Output = "Output";
}
