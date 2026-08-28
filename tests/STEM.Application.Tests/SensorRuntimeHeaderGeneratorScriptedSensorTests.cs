using STEM.Application.Dtos.Simulation;
using STEM.Application.UseCases.Simulation;
using STEM.Application.UseCases.Simulation.Runners.Qemu;

namespace STEM.Application.Tests;

// RUNTIME + INTERACTIVE COVERAGE BOOST milestone. These sensor types already
// had real, working, ENABLED (SimulationRunner:Qemu:EnableSensorInputScenario
// = true in appsettings.json) QEMU scripted-scenario support in
// SensorRuntimeHeaderGenerator.cs before this milestone touched anything —
// confirmed by source read, then locked in here since no test previously
// covered them (only DHT had a dedicated test file). IR Obstacle Sensor and
// single-channel Line Tracking Sensor are the two genuinely NEW additions
// this milestone (2-line GenericSensorPins entries, same proven pattern).
public sealed class SensorRuntimeHeaderGeneratorScriptedSensorTests
{
    [Fact]
    public void Generate_HcSr04WithScenario_EmitsPulseInUsing58UsPerCmFormula()
    {
        var snapshot = new VirtualLabRuntimeDiagramSnapshot(new List<VirtualLabRuntimeComponent>
        {
            new("hc1", "wokwi-hc-sr04", new Dictionary<string, string> { ["TRIG"] = "GPIO5", ["ECHO"] = "GPIO18" })
        });
        var scenario = new SensorScenarioConfig
        {
            Sensors = new Dictionary<string, SensorTimeline>
            {
                ["hc1"] = new SensorTimeline
                {
                    Type = "wokwi-hc-sr04",
                    Timeline = new List<SensorTimelineEntry> { new() { TimeMs = 0, DistanceCm = 20 } }
                }
            }
        };

        var header = SensorRuntimeHeaderGenerator.Generate(snapshot, scenario);

        Assert.NotNull(header);
        Assert.Contains("case GPIO18:", header); // ECHO pin drives the pulseIn wrapper
        Assert.Contains("* 58.0f", header); // real speed-of-sound conversion, not invented
        Assert.Contains("20.0f", header); // timeline value flows through
    }

    [Fact]
    public void Generate_PirWithScenario_EmitsMotionLookupOnOutPin()
    {
        var snapshot = new VirtualLabRuntimeDiagramSnapshot(new List<VirtualLabRuntimeComponent>
        {
            new("pir1", "wokwi-pir-motion-sensor", new Dictionary<string, string> { ["OUT"] = "GPIO13" })
        });
        var scenario = new SensorScenarioConfig
        {
            Sensors = new Dictionary<string, SensorTimeline>
            {
                ["pir1"] = new SensorTimeline
                {
                    Type = "wokwi-pir-motion-sensor",
                    Timeline = new List<SensorTimelineEntry> { new() { TimeMs = 0, Motion = true } }
                }
            }
        };

        var header = SensorRuntimeHeaderGenerator.Generate(snapshot, scenario);

        Assert.NotNull(header);
        Assert.Contains("case GPIO13:", header);
        Assert.Contains("__sf_lookupBool", header);
    }

    [Theory]
    [InlineData("wokwi-water-leak-sensor", "S")]
    [InlineData("wokwi-flame-sensor", "DOUT")]
    [InlineData("wokwi-soil-moisture-sensor", "DO")]
    [InlineData("wokwi-rain-sensor", "DO")]
    [InlineData("wokwi-vibration-sensor", "OUT")]
    [InlineData("wokwi-ir-obstacle-sensor", "OUT")]
    [InlineData("wokwi-line-tracking-sensor", "OUT")]
    public void Generate_GenericDigitalSensorFamily_EmitsDetectedLookupOnCorrectPin(string simulationType, string pinName)
    {
        var snapshot = new VirtualLabRuntimeDiagramSnapshot(new List<VirtualLabRuntimeComponent>
        {
            new("s1", simulationType, new Dictionary<string, string> { [pinName] = "GPIO27" })
        });
        var scenario = new SensorScenarioConfig
        {
            Sensors = new Dictionary<string, SensorTimeline>
            {
                ["s1"] = new SensorTimeline
                {
                    Type = simulationType,
                    Timeline = new List<SensorTimelineEntry> { new() { TimeMs = 0, Detected = true } }
                }
            }
        };

        var header = SensorRuntimeHeaderGenerator.Generate(snapshot, scenario);

        Assert.NotNull(header);
        Assert.Contains("case GPIO27:", header);
    }

    [Theory]
    [InlineData("wokwi-line-tracking-3ch", 3)]
    [InlineData("wokwi-line-tracking-5ch", 5)]
    public void Generate_LineTrackingMultiChannel_EmitsOneCaseBranchPerChannel(string simulationType, int channelCount)
    {
        var pins = new Dictionary<string, string>();
        for (var i = 0; i < channelCount; i++) pins[$"OUT{i + 1}"] = $"GPIO{10 + i}";

        var snapshot = new VirtualLabRuntimeDiagramSnapshot(new List<VirtualLabRuntimeComponent>
        {
            new("lt1", simulationType, pins)
        });
        var scenario = new SensorScenarioConfig
        {
            Sensors = new Dictionary<string, SensorTimeline>
            {
                ["lt1"] = new SensorTimeline
                {
                    Type = simulationType,
                    Timeline = new List<SensorTimelineEntry> { new() { TimeMs = 0, Pattern = "center" } }
                }
            }
        };

        var header = SensorRuntimeHeaderGenerator.Generate(snapshot, scenario);

        Assert.NotNull(header);
        for (var i = 0; i < channelCount; i++)
        {
            Assert.Contains($"case GPIO{10 + i}:", header);
        }
    }

    [Fact]
    public void Generate_UnwiredSensor_SkipsComponentEntirely()
    {
        // Missing the required pin(s) entirely — must not throw, must not
        // emit a bogus case for a pin that doesn't exist.
        var snapshot = new VirtualLabRuntimeDiagramSnapshot(new List<VirtualLabRuntimeComponent>
        {
            new("pir1", "wokwi-pir-motion-sensor", new Dictionary<string, string>())
        });

        var header = SensorRuntimeHeaderGenerator.Generate(snapshot, scenario: null);

        Assert.Null(header);
    }
}
