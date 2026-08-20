using System.Text.Json;
using STEM.Application.UseCases.Components;

namespace STEM.Application.Tests;

// STEP 9 of the Component Compatibility Matrix milestone: the matrix
// (component-compatibility.json, repo root) is a hand-curated snapshot from a
// full source audit, not machine-generated — full auto-generation would need
// reflecting into EducationalEventGenerator's private model-index
// dictionaries, out of scope for this measurement-only milestone (STEP 10).
// What CAN be checked automatically is checked here: every entry's claims
// are verified against the actually-callable resolvers
// (RuntimeCapabilityResolver, SimulationTypeResolver), so the matrix can't
// silently drift from what the code really does for the parts of it that
// have a live source of truth to compare against.
public sealed class ComponentCompatibilityMatrixTests
{
    private static readonly Lazy<MatrixDocument> Matrix = new(LoadMatrix);

    private static MatrixDocument LoadMatrix()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8 && dir != null; i++)
        {
            var candidate = Path.Combine(dir, "component-compatibility.json");
            if (File.Exists(candidate))
            {
                var json = File.ReadAllText(candidate);
                return JsonSerializer.Deserialize<MatrixDocument>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                })!;
            }

            dir = Path.GetDirectoryName(dir);
        }

        throw new FileNotFoundException(
            "component-compatibility.json not found by walking up from the test assembly's output directory — " +
            "expected at repo root (STEM_BE/component-compatibility.json).");
    }

    [Fact]
    public void Matrix_LoadsAndHasEntries()
    {
        Assert.NotEmpty(Matrix.Value.Components);
    }

    // Class A invariant (STEP 9): SimulationComponentType != null, runtime
    // capability != empty, AND RuntimeCapabilityResolver independently agrees.
    [Fact]
    public void ClassA_Entries_HaveNonEmptyCapabilityConfirmedByResolver()
    {
        var classA = Matrix.Value.Components.Where(c => c.Classification == "A");
        Assert.NotEmpty(classA);

        foreach (var entry in classA)
        {
            Assert.NotNull(entry.SimulationComponentType);
            Assert.NotEmpty(entry.RuntimeCapabilities);

            var resolved = RuntimeCapabilityResolver.Resolve(entry.SimulationComponentType);
            Assert.NotNull(resolved);
            Assert.Contains(resolved!.Capability, entry.RuntimeCapabilities);
        }
    }

    // Class B invariant: SimulationComponentType != null. Some Class B
    // entries (QEMU-only: l298n, static rgb-led) are a documented
    // RuntimeCapabilityResolver gap (matrix's own missingRequirements says
    // so) — this test only asserts the parts of the invariant that hold
    // today, it does not assert the resolver has an entry for them.
    [Fact]
    public void ClassB_Entries_HaveSimulationComponentType()
    {
        var classB = Matrix.Value.Components.Where(c => c.Classification == "B");
        Assert.NotEmpty(classB);

        foreach (var entry in classB)
        {
            Assert.NotNull(entry.SimulationComponentType);
        }
    }

    // Class E invariant (STEP 9's explicit rule): "không được quảng cáo
    // Simulation Ready" — SimulationComponentType must be null and
    // RuntimeCapabilities must be empty, confirmed independently via
    // RuntimeCapabilityResolver too.
    [Fact]
    public void ClassE_Entries_NeverAdvertiseSimulationReady()
    {
        var classE = Matrix.Value.Components.Where(c => c.Classification == "E");
        Assert.NotEmpty(classE);

        foreach (var entry in classE)
        {
            Assert.Null(entry.SimulationComponentType);
            Assert.Empty(entry.RuntimeCapabilities);
            Assert.Null(RuntimeCapabilityResolver.Resolve(entry.SimulationComponentType));
        }
    }

    // The two named regression cases from STEP 4/STEP 11 must be present in
    // the matrix and specifically classified E, by name — not just "some E
    // entries exist somewhere".
    [Theory]
    [InlineData("led.rgb-led-4-legs")]
    [InlineData("component.apa-106-f5")]
    public void KnownFalsePositiveCandidates_AreClassifiedE(string canonicalKey)
    {
        var entry = Matrix.Value.Components.SingleOrDefault(c => c.CanonicalKey == canonicalKey);
        Assert.NotNull(entry);
        Assert.Equal("E", entry!.Classification);
    }

    // wokwi-rgb-led (the STATIC catalog's own canonical type, real
    // RgbLedModel-backed) must stay a real, distinct entity from the
    // imported "RGB LED - (4 legs)" row above — this is the regression this
    // whole matrix exists to keep visible: same human name, different
    // simulation identity, must never collapse into one.
    [Fact]
    public void StaticRgbLed_IsDistinctFromImportedRgbLed_AndIsClassB()
    {
        var staticEntry = Matrix.Value.Components.Single(c => c.CanonicalKey == "wokwi-rgb-led");
        var importedEntry = Matrix.Value.Components.Single(c => c.CanonicalKey == "led.rgb-led-4-legs");

        Assert.Equal("B", staticEntry.Classification);
        Assert.Equal("wokwi-rgb-led", staticEntry.SimulationComponentType);
        Assert.Equal("E", importedEntry.Classification);
        Assert.Null(importedEntry.SimulationComponentType);
    }

    private sealed class MatrixDocument
    {
        public List<MatrixEntry> Components { get; set; } = new();
    }

    private sealed class MatrixEntry
    {
        public string CanonicalKey { get; set; } = string.Empty;
        public string? SimulationComponentType { get; set; }
        public List<string> RuntimeCapabilities { get; set; } = new();
        public string Classification { get; set; } = string.Empty;
    }
}
