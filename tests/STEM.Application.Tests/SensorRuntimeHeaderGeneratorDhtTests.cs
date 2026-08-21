using STEM.Application.Dtos.Simulation;
using STEM.Application.UseCases.Simulation;
using STEM.Application.UseCases.Simulation.Runners.Qemu;

namespace STEM.Application.Tests;

// DHT22/DHT11 QEMU matrix-correction milestone. Confirms, against the real
// resolver (not a hand-typed literal in component-compatibility.json), that:
//   1) SensorRuntimeHeaderGenerator DOES generate a real StemFlowDHT C++
//      model for both wokwi-dht22 and wokwi-dht11, sourced from the
//      diagram's sensorScenario timeline (SensorScenarioDtos.cs) — the
//      matrix's old "No runtime model in either runner" claim was false.
//   2) The generator does NOT itself provide a "StemFlowDHT.h" file/include
//      guard — it only emits an inline `class StemFlowDHT { ... }`
//      definition. This is the real, live-verified (Docker/arduino-cli,
//      this milestone) root cause of the current compile-contract bug: any
//      student code following the documented contract (and the sample
//      exercise, virtualLabSampleExercises.ts Bai 10) writes
//      `#include "StemFlowDHT.h"`, but SimulationCompileService only ever
//      writes a single sketch.ino to the sandbox (no such header file
//      exists) — real failure: "fatal error: StemFlowDHT.h: No such file or
//      directory". This test locks in that the generator's OWN output
//      never satisfies that include, so nobody "fixes" this bug by
//      accident without this test forcing a conscious update.
public sealed class SensorRuntimeHeaderGeneratorDhtTests
{
    [Theory]
    [InlineData("wokwi-dht22", "dht_a")]
    [InlineData("wokwi-dht11", "dht_b")]
    public void Generate_DhtComponentWithScenario_EmitsRealStemFlowDhtClass(string simulationType, string componentId)
    {
        var snapshot = new VirtualLabRuntimeDiagramSnapshot(new List<VirtualLabRuntimeComponent>
        {
            new(componentId, simulationType, new Dictionary<string, string> { ["SDA"] = "GPIO19" })
        });

        var scenario = new SensorScenarioConfig
        {
            Sensors = new Dictionary<string, SensorTimeline>
            {
                [componentId] = new SensorTimeline
                {
                    Type = simulationType,
                    Timeline = new List<SensorTimelineEntry>
                    {
                        new() { TimeMs = 0, Temperature = 25, Humidity = 55 },
                        new() { TimeMs = 5000, Temperature = 30, Humidity = 50 },
                        new() { TimeMs = 9000, Temperature = 38, Humidity = 40 },
                    }
                }
            }
        };

        var header = SensorRuntimeHeaderGenerator.Generate(snapshot, scenario);

        Assert.NotNull(header);
        Assert.Contains("class StemFlowDHT", header);
        Assert.Contains("float readTemperature()", header);
        Assert.Contains("float readHumidity()", header);
        Assert.Contains($"strcmp(_id, \"{componentId}\") == 0", header);
        // Timeline values really flow through — not just a stub.
        Assert.Contains("38.0f", header);
        Assert.Contains("40.0f", header);
    }

    [Fact]
    public void Generate_DhtScenario_NeverEmitsAStemFlowDhtHeaderFile()
    {
        // Root-cause regression lock: the generator only ever inlines a
        // `class StemFlowDHT { ... }` definition — it never writes or
        // #includes an actual "StemFlowDHT.h" file. Combined with
        // SimulationCompileService only writing a single sketch.ino to the
        // sandbox (STEM.Infrastructure/Services/SimulationCompileService.cs),
        // any student code containing `#include "StemFlowDHT.h"` (the
        // documented contract) fails to compile today — live-verified via
        // real Docker/arduino-cli compile this milestone, not simulated
        // here (no Docker dependency in this test). If this assertion ever
        // starts failing because the generator began emitting/providing
        // that header file, the missing-stub bug this milestone documented
        // would be fixed — update component-compatibility.json's DHT
        // reason/missingRequirements accordingly instead of just deleting
        // this test.
        var snapshot = new VirtualLabRuntimeDiagramSnapshot(new List<VirtualLabRuntimeComponent>
        {
            new("dht1", "wokwi-dht22", new Dictionary<string, string> { ["SDA"] = "GPIO19" })
        });
        var scenario = new SensorScenarioConfig
        {
            Sensors = new Dictionary<string, SensorTimeline>
            {
                ["dht1"] = new SensorTimeline { Type = "wokwi-dht22", Timeline = new List<SensorTimelineEntry> { new() { TimeMs = 0, Temperature = 25, Humidity = 55 } } }
            }
        };

        var header = SensorRuntimeHeaderGenerator.Generate(snapshot, scenario);

        Assert.NotNull(header);
        // The generator's own explanatory comment DOES mention the string
        // "StemFlowDHT.h" (telling students they must add the #include
        // themselves) — that's fine. What must never appear is the
        // generator adding the #include DIRECTIVE itself, or defining the
        // header file's content in any way other than the inline class —
        // that's the literal, precise bug this test locks in.
        Assert.DoesNotContain("#include \"StemFlowDHT.h\"", header);
    }

    [Fact]
    public void Generate_NoComponentsAndNoScenario_ReturnsNull()
    {
        var snapshot = new VirtualLabRuntimeDiagramSnapshot(new List<VirtualLabRuntimeComponent>());

        var header = SensorRuntimeHeaderGenerator.Generate(snapshot, scenario: null);

        Assert.Null(header);
    }
}
