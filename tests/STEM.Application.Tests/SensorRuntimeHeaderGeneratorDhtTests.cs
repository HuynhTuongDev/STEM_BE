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
//      definition. This USED TO BE the real, live-verified (Docker/
//      arduino-cli) root cause of a real compile failure: any student code
//      following the documented contract (and the sample exercise,
//      virtualLabSampleExercises.ts Bai 10) writes
//      `#include "StemFlowDHT.h"`, but SimulationCompileService only ever
//      wrote a single sketch.ino to the sandbox (no such header file
//      existed) — real failure: "fatal error: StemFlowDHT.h: No such file or
//      directory".
//
// FIXED 2026-08-23 — NOT by changing Generate() (still intentionally inlines
// the class only, see test below), but by adding a sibling method,
// SensorRuntimeHeaderGenerator.BuildExtraFiles(), that returns a real (but
// intentionally EMPTY — include-guard only, no redefinition) "StemFlowDHT.h"
// stub whenever a DHT11/DHT22 component is present. QemuEsp32Runner passes
// this through IFirmwareCacheService.CompileAndCacheAsync's new `extraFiles`
// parameter down to CompileSimulationRequest.ExtraFiles, and
// SimulationCompileService now writes every entry of ExtraFiles into the
// sketch sandbox directory alongside sketch.ino before invoking arduino-cli
// — so the student's `#include "StemFlowDHT.h"` now resolves to a real file,
// while the actual class (with live Sensor Scenario timeline data) keeps
// coming from Generate()'s inlined text earlier in the same sketch.ino
// translation unit. See SensorRuntimeHeaderGeneratorDhtExtraFilesTests.cs for
// the tests locking in the fix itself.
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
        // Generate()'s own contract stays unchanged by the 2026-08-23 fix: it
        // only ever inlines a `class StemFlowDHT { ... }` definition — it
        // never writes or #includes an actual "StemFlowDHT.h" file itself.
        // That part of the job now belongs to the sibling method
        // SensorRuntimeHeaderGenerator.BuildExtraFiles() (see
        // SensorRuntimeHeaderGeneratorDhtExtraFilesTests.cs), which
        // SimulationCompileService writes into the sandbox alongside
        // sketch.ino. If this assertion ever starts failing because
        // Generate() itself began emitting the #include directive or the
        // header file's content, update this comment (and check
        // BuildExtraFiles/SimulationCompileService for an unintended
        // duplicate/conflicting definition) instead of just deleting this
        // test.
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
