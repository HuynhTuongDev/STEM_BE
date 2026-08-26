using STEM.Application.Dtos.Simulation;
using STEM.Application.UseCases.Simulation;
using STEM.Application.UseCases.Simulation.Runners.Qemu;

namespace STEM.Application.Tests;

// Locks in the actual fix for the DHT22/DHT11 "fatal error: StemFlowDHT.h: No
// such file or directory" compile bug (see SensorRuntimeHeaderGeneratorDhtTests
// for the historical root-cause write-up). BuildExtraFiles() is the piece that
// makes the student's `#include "StemFlowDHT.h"` resolve to a real file —
// generic over ANY DHT11/DHT22 component (not hardcoded to a specific lab).
public sealed class SensorRuntimeHeaderGeneratorDhtExtraFilesTests
{
    [Theory]
    [InlineData("wokwi-dht22")]
    [InlineData("wokwi-dht11")]
    public void BuildExtraFiles_DhtComponentPresent_ReturnsStemFlowDhtHStub(string simulationType)
    {
        var snapshot = new VirtualLabRuntimeDiagramSnapshot(new List<VirtualLabRuntimeComponent>
        {
            new("dht1", simulationType, new Dictionary<string, string> { ["SDA"] = "GPIO19" })
        });

        var extraFiles = SensorRuntimeHeaderGenerator.BuildExtraFiles(snapshot);

        Assert.NotNull(extraFiles);
        Assert.True(extraFiles!.ContainsKey("StemFlowDHT.h"));
        var content = extraFiles["StemFlowDHT.h"];
        Assert.Contains("#ifndef", content);
        Assert.Contains("#define", content);
        Assert.Contains("#endif", content);
        // Must be an empty stub — the REAL class definition lives in
        // Generate()'s inlined output earlier in the same sketch.ino
        // translation unit. Redefining it here would cause a
        // "redefinition of 'class StemFlowDHT'" compile error.
        Assert.DoesNotContain("class StemFlowDHT {", content);
        Assert.DoesNotContain("readTemperature", content);
        Assert.DoesNotContain("readHumidity", content);
    }

    [Fact]
    public void BuildExtraFiles_NoDhtComponent_ReturnsNull()
    {
        var snapshot = new VirtualLabRuntimeDiagramSnapshot(new List<VirtualLabRuntimeComponent>
        {
            new("hc1", "wokwi-hc-sr04", new Dictionary<string, string> { ["TRIG"] = "GPIO5", ["ECHO"] = "GPIO18" })
        });

        var extraFiles = SensorRuntimeHeaderGenerator.BuildExtraFiles(snapshot);

        Assert.Null(extraFiles);
    }

    [Fact]
    public void BuildExtraFiles_EmptySnapshot_ReturnsNull()
    {
        var snapshot = new VirtualLabRuntimeDiagramSnapshot(new List<VirtualLabRuntimeComponent>());

        var extraFiles = SensorRuntimeHeaderGenerator.BuildExtraFiles(snapshot);

        Assert.Null(extraFiles);
    }

    [Fact]
    public void CompileSimulationRequest_ExtraFiles_DefaultsToNull()
    {
        // Backward-compat guard: every existing caller that never sets
        // ExtraFiles (the public POST api/simulation/compile endpoint,
        // precompile-on-save, etc.) must keep writing only sketch.ino.
        var request = new CompileSimulationRequest();

        Assert.Null(request.ExtraFiles);
    }
}
