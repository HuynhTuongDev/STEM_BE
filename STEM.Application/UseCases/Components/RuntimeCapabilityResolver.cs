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

            // QEMU-only, no RuntimeCapabilityResolver entry until now — a real
            // gap independently confirmed by both the L298N and RGB LED
            // matrix entries' own "missingRequirements" notes. Output-only:
            // the component reacts to digitalWrite from firmware, no live
            // input.
            ["wokwi-l298n"] = new(RuntimeCapabilities.Output, null),
            ["wokwi-rgb-led"] = new(RuntimeCapabilities.Output, null),

            // RUNTIME + INTERACTIVE COVERAGE BOOST milestone — these all
            // already had real, working, ENABLED (SimulationRunner:Qemu:
            // EnableSensorInputScenario=true in appsettings.json) scripted
            // scenario support in SensorRuntimeHeaderGenerator.cs; this
            // resolver entry was simply never added, so CircuitCanvas/the
            // palette had no way to know a scenario-configuration UI applies
            // (see RuntimeCapabilities.ScriptedSensor's own doc comment for
            // why this is NOT the same as SensorInput/live-realtime).
            ["wokwi-hc-sr04"] = new(RuntimeCapabilities.ScriptedSensor, "ultrasonic-distance"),
            ["wokwi-pir-motion-sensor"] = new(RuntimeCapabilities.ScriptedSensor, "motion"),
            ["wokwi-line-tracking-sensor"] = new(RuntimeCapabilities.ScriptedSensor, "line"),
            ["wokwi-line-tracking-3ch"] = new(RuntimeCapabilities.ScriptedSensor, "line"),
            ["wokwi-line-tracking-5ch"] = new(RuntimeCapabilities.ScriptedSensor, "line"),
            ["wokwi-water-leak-sensor"] = new(RuntimeCapabilities.ScriptedSensor, "water-leak"),
            ["wokwi-flame-sensor"] = new(RuntimeCapabilities.ScriptedSensor, "flame"),
            ["wokwi-soil-moisture-sensor"] = new(RuntimeCapabilities.ScriptedSensor, "soil-moisture"),
            ["wokwi-rain-sensor"] = new(RuntimeCapabilities.ScriptedSensor, "rain"),
            ["wokwi-vibration-sensor"] = new(RuntimeCapabilities.ScriptedSensor, "vibration"),
            ["wokwi-ir-obstacle-sensor"] = new(RuntimeCapabilities.ScriptedSensor, "obstacle"),
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

    /// <summary>
    /// A sensor whose value comes from a pre-configured timeline baked into
    /// the firmware at compile time (see SensorRuntimeHeaderGenerator.cs /
    /// EducationalRunState's DHT-scenario reuse), NOT from a live FE control
    /// forwarded through ISimulationInputChannel while the simulation runs.
    /// Deliberately distinct from SensorInput (photoresistor: a real-time
    /// slider/light value pushed live via SignalR during a running session).
    /// The FE should render a "configure scenario" UI for this capability,
    /// not a live toggle/slider — see CircuitCanvas capability-driven UI.
    /// </summary>
    public const string ScriptedSensor = "ScriptedSensor";
}
