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

    // Relay Module milestone (Class D -> B): must be classified B, with a
    // real SimulationComponentType, Output-only capability (never
    // DigitalInput — Relay is not an input component), and
    // RuntimeCapabilityResolver must independently agree, same shape as the
    // ClassA invariant check above.
    [Fact]
    public void Relay_IsClassB_WithOutputOnlyCapabilityConfirmedByResolver()
    {
        var entry = Matrix.Value.Components.Single(c => c.CanonicalKey == "wokwi-relay-module");

        Assert.Equal("B", entry.Classification);
        Assert.Equal("wokwi-relay-module", entry.SimulationComponentType);
        Assert.Equal(new[] { "Output" }, entry.RuntimeCapabilities);

        var resolved = RuntimeCapabilityResolver.Resolve(entry.SimulationComponentType);
        Assert.NotNull(resolved);
        Assert.Equal(RuntimeCapabilities.Output, resolved!.Capability);
    }

    // Verified External Component Assets milestone — PIR Motion Sensor
    // vertical slice (Class D -> C). Visual + pin geometry were already real
    // (wokwi-pir-motion-sensor, @wokwi/elements pinInfo, MIT) before this
    // milestone; the wiring rule added this milestone is what earns the
    // classification move, per the matrix's own C-vs-D legend (dedicated
    // wiring rule exists), independent of runtime capability (Phase 23:
    // "Không thay classification A-E nếu runtime capability không đổi" —
    // this move is legitimate because dedicatedWiringRule changed, not
    // because any runtime capability did).
    [Fact]
    public void Pir_IsClassC_WithVerifiedVisualAndPinGeometry()
    {
        var entry = Matrix.Value.Components.Single(c => c.CanonicalKey == "wokwi-pir-motion-sensor");

        Assert.Equal("C", entry.Classification);
        Assert.True(entry.DedicatedWiringRule);
        Assert.True(entry.VisualAssetVerified);
        Assert.True(entry.PinGeometryVerified);
        Assert.True(entry.CanvasWiringReady);
        Assert.Equal("@wokwi/elements", entry.AssetProvider);
        // Still no live/interactive capability — this milestone is
        // visual/pin sourcing, not simulation expansion (Phase 24).
        Assert.Empty(entry.RuntimeCapabilities);
    }

    // PIN_UNVERIFIED negative-test case (Phase 20/22). pH Sensor's
    // VCC/GND/PO were invented to fit the fallback card, never checked
    // against any provider metadata, datasheet, or manufacturer doc — and
    // "pH Sensor" itself is an unresolved identity question (probe vs
    // interface board). Must never claim wiring-readiness while that's
    // true: this test locks CanvasWiringReady/PinGeometryVerified/
    // VisualAssetVerified all false, and Classification NOT C or above,
    // so nobody flips pH to "wiring-validation" by editing the JSON alone
    // without also attaching real evidence first.
    [Fact]
    public void PhSensor_IsPinUnverified_NeverClaimsWiringReady()
    {
        var entry = Matrix.Value.Components.Single(c => c.CanonicalKey == "wokwi-ph-sensor");

        Assert.False(entry.VisualAssetVerified);
        Assert.False(entry.PinGeometryVerified);
        Assert.False(entry.CanvasWiringReady);
        Assert.False(entry.DedicatedWiringRule);
        Assert.DoesNotContain(entry.Classification, new[] { "A", "B", "C" });
    }

    // Phase 21 regression (raw LDR vs Light Sensor Module) is already
    // covered by SimulationTypeResolverTests.
    // Resolve_PassiveComponentCategoriesWithNoBreakoutModuleIdentity_NeverMapped
    // — re-asserted here from the matrix side: the STATIC wokwi-photoresistor-sensor
    // entry (a real module: VCC/GND/DO/AO) must stay a real, distinct A-class
    // entry, independent of whatever a raw-LDR import attempt resolves to.
    [Fact]
    public void PhotoresistorModule_StaysClassA_IndependentOfRawLdrRegression()
    {
        var entry = Matrix.Value.Components.Single(c => c.CanonicalKey == "wokwi-photoresistor-sensor");

        Assert.Equal("A", entry.Classification);
        Assert.True(entry.VisualAssetVerified);
        Assert.True(entry.PinGeometryVerified);
        Assert.Equal("@wokwi/elements", entry.AssetProvider);
    }

    // Component Source Resolution milestone — Soil Moisture Sensor. A
    // genuinely NEW middle state, distinct from both PIR (fully verified)
    // and pH (nothing verified): pin SEMANTICS verified (cross-vendor
    // corroborated, no CAD/visual asset needed for that) + a real
    // dedicated wiring rule, but pin GEOMETRY still unverified (no matching
    // visual/CAD asset found anywhere checked). Locks that
    // PinDefinitionVerified and PinGeometryVerified can legitimately
    // disagree — a wiring rule doesn't require verified geometry — while
    // CanvasWiringReady still correctly stays false (Phase 17's badge rule
    // needs ALL THREE: PinDefinitionVerified, PinGeometryVerified, AND
    // DedicatedWiringRule).
    [Fact]
    public void SoilMoisture_HasVerifiedSemanticsAndWiringRule_ButNotVerifiedGeometry()
    {
        var entry = Matrix.Value.Components.Single(c => c.CanonicalKey == "wokwi-soil-moisture-sensor");

        Assert.Equal("C", entry.Classification);
        Assert.True(entry.DedicatedWiringRule);
        Assert.True(entry.PinDefinitionVerified);
        Assert.False(entry.PinGeometryVerified);
        Assert.False(entry.VisualAssetVerified);
        Assert.False(entry.CanvasWiringReady);
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
        public bool DedicatedWiringRule { get; set; }
        public string? VisualAsset { get; set; }
        public bool? VisualAssetVerified { get; set; }
        public bool PinDefinition { get; set; }
        public bool? PinDefinitionVerified { get; set; }
        public bool PinGeometry { get; set; }
        public bool? PinGeometryVerified { get; set; }
        public bool CanvasWiringReady { get; set; }
        public string? AssetProvider { get; set; }
        public string? PinProvider { get; set; }
    }
}
