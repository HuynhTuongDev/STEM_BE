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

    // Class B invariant: the component must be simulation-capable via SOME
    // real path — either SimulationTypeResolver resolved a
    // SimulationComponentType (the Registry-import path), OR it's a native
    // static-catalog type with real, confirmed QEMU support (QemuSupport is
    // truthy) — SimulationTypeResolver is exclusively about resolving a
    // GENERIC IMPORTED part's category to a canonical type; native catalog
    // types (registrySource.imported=false) never go through it and
    // legitimately have a null SimulationComponentType regardless of how
    // real their runtime is (RUNTIME + INTERACTIVE COVERAGE BOOST milestone
    // — this widened an exception that used to cover only l298n/rgb-led to
    // the 11 scripted-sensor types found in the same situation).
    [Fact]
    public void ClassB_Entries_AreSimulationCapableViaResolverOrConfirmedQemu()
    {
        var classB = Matrix.Value.Components.Where(c => c.Classification == "B");
        Assert.NotEmpty(classB);

        foreach (var entry in classB)
        {
            var hasResolvedType = entry.SimulationComponentType != null;
            var hasConfirmedQemu = entry.QemuSupport.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.String => true, // e.g. "L298nModel.cs" — a named model IS confirmed support
                _ => false
            };
            Assert.True(hasResolvedType || hasConfirmedQemu,
                $"{entry.CanonicalKey} is Class B but has neither a resolved SimulationComponentType nor confirmed QemuSupport");
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
    public void Pir_IsClassB_WithVerifiedVisualPinGeometryAndScriptedRuntime()
    {
        var entry = Matrix.Value.Components.Single(c => c.CanonicalKey == "wokwi-pir-motion-sensor");

        // RUNTIME + INTERACTIVE COVERAGE BOOST milestone: moved from C to B
        // — the real, already-existing QEMU scripted support (and this
        // milestone's new Educational port) is now credited.
        Assert.Equal("B", entry.Classification);
        Assert.True(entry.DedicatedWiringRule);
        Assert.True(entry.VisualAssetVerified);
        Assert.True(entry.PinGeometryVerified);
        Assert.True(entry.CanvasWiringReady);
        Assert.Equal("@wokwi/elements", entry.AssetProvider);
        Assert.Contains("ScriptedSensor", entry.RuntimeCapabilities);
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

    // Component Source Resolution milestone — Soil Moisture Sensor. Pin
    // SEMANTICS were verified first (cross-vendor corroborated) + a real
    // dedicated wiring rule added, while pin GEOMETRY/visual stayed
    // unassessed. VISUAL REFINEMENT / EXPANSION phase closed that gap: the
    // existing fallback-card SVG was re-checked against the real YL-69
    // module and found already accurate, so all four flags — and
    // CanvasWiringReady — are true now. Locks in that outcome.
    [Fact]
    public void SoilMoisture_NowFullyVerified_IncludingGeometryAndCanvasWiringReady()
    {
        var entry = Matrix.Value.Components.Single(c => c.CanonicalKey == "wokwi-soil-moisture-sensor");

        // RUNTIME + INTERACTIVE COVERAGE BOOST milestone: moved from C to B
        // — real, already-existing QEMU scripted support (and this
        // milestone's new Educational port) is now credited.
        Assert.Equal("B", entry.Classification);
        Assert.True(entry.DedicatedWiringRule);
        Assert.True(entry.PinDefinitionVerified);
        Assert.True(entry.PinGeometryVerified);
        Assert.True(entry.VisualAssetVerified);
        Assert.True(entry.CanvasWiringReady);
        Assert.Equal("VERIFIED_INTERNAL_VISUAL", entry.VisualCapability);
        Assert.Contains("ScriptedSensor", entry.RuntimeCapabilities);
    }

    // REAL COMPONENT VISUAL milestone — visualCapability is a NEW, independent
    // axis (Golden Rule: "Visual-ready != Simulation-ready"). Every entry must
    // carry one of exactly 3 values, and the value must be internally
    // consistent with the asset fields it's derived from.
    private static readonly HashSet<string> ValidVisualCapabilities = new()
    {
        "REAL_PROVIDER_VISUAL", "VERIFIED_INTERNAL_VISUAL", "GENERIC_FALLBACK"
    };

    [Fact]
    public void EveryEntry_HasAValidVisualCapability()
    {
        foreach (var entry in Matrix.Value.Components)
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.VisualCapability), $"{entry.CanonicalKey} is missing visualCapability");
            Assert.Contains(entry.VisualCapability!, ValidVisualCapabilities);
        }
    }

    [Fact]
    public void RealProviderVisual_Entries_AreAlwaysWokwiElementsAndVerified()
    {
        var realProvider = Matrix.Value.Components.Where(c => c.VisualCapability == "REAL_PROVIDER_VISUAL");
        Assert.NotEmpty(realProvider);

        foreach (var entry in realProvider)
        {
            Assert.True(entry.VisualAssetVerified, $"{entry.CanonicalKey} claims REAL_PROVIDER_VISUAL without VisualAssetVerified");
            Assert.Equal("@wokwi/elements", entry.AssetProvider);
        }
    }

    [Fact]
    public void GenericFallback_Entries_NeverClaimAWokwiElementsAsset()
    {
        var fallback = Matrix.Value.Components.Where(c => c.VisualCapability == "GENERIC_FALLBACK");
        Assert.NotEmpty(fallback);

        foreach (var entry in fallback)
        {
            Assert.NotEqual("@wokwi/elements", entry.AssetProvider);
        }
    }

    // P0 components (REAL COMPONENT VISUAL milestone, STEP 9): 12 parts serve
    // the current lab set, but ESP32 DevKit V1 is the BOARD itself (handled
    // by getBoardTagName in CircuitCanvas.tsx, not a placeable peripheral) and
    // has no entry in this component-only matrix — out of scope for this
    // file, not a gap. Of the 11 remaining, 9 already have real visual + real
    // pin geometry via @wokwi/elements (verified against the actual shipped
    // package during this milestone's audit, not re-derived here). Relay
    // Module and L298N are the only 2 genuine gaps — audited and confirmed
    // unfixable via the officially-configured Fritzing repo (no "relay
    // module"/"L298N" part exists there; @wokwi/elements' `ks2e-m-dc5` is a
    // raw dual relay, wrong identity for "Relay Module" — same raw-vs-module
    // trap as the earlier LDR/pH cases). Their pin NAMES were cross-verified
    // against components101.com / lastminuteengineers.com / circuitdigest.com
    // this milestone even though the visual stays generic.
    [Theory]
    [InlineData("wokwi-led")]
    [InlineData("wokwi-pushbutton")]
    [InlineData("wokwi-buzzer")]
    [InlineData("wokwi-servo")]
    [InlineData("wokwi-potentiometer")]
    [InlineData("wokwi-photoresistor-sensor")]
    [InlineData("wokwi-dht11")]
    [InlineData("wokwi-dht22")]
    [InlineData("wokwi-hc-sr04")]
    public void P0_RealVisualComponents_AreClassifiedRealProviderVisual(string canonicalKey)
    {
        var entry = Matrix.Value.Components.SingleOrDefault(c => c.CanonicalKey == canonicalKey);
        Assert.NotNull(entry);
        Assert.Equal("REAL_PROVIDER_VISUAL", entry!.VisualCapability);
    }

    // VISUAL REFINEMENT / EXPANSION phase superseded this: Relay Module and
    // L298N were re-assessed and their existing fallback-card SVGs judged to
    // already be reference-accurate (green PCB + terminal rows + heatsink for
    // L298N; blue PCB + relay-chip block for Relay) — see
    // P0_GapComponents_NowVerifiedInternalVisual_WithCanvasWiringReady below
    // for the current, correct assertion. Pin names stay cross-verified
    // regardless (checked here too, since that fact didn't change).
    [Theory]
    [InlineData("wokwi-relay-module")]
    [InlineData("wokwi-l298n")]
    public void P0_GapComponents_HavePinNamesVerified(string canonicalKey)
    {
        var entry = Matrix.Value.Components.SingleOrDefault(c => c.CanonicalKey == canonicalKey);
        Assert.NotNull(entry);
        Assert.True(entry!.PinDefinitionVerified, $"{canonicalKey} pin names should be cross-verified");
    }

    // Full-catalog coverage snapshot (STEP 20): locks in the current honest
    // number so future drift (up OR down) is visible in a diff, without
    // hard-failing CI on a target this milestone explicitly does not require
    // for the full 71-component catalog (only for the 12 P0 parts above).
    [Fact]
    public void FullCatalog_VisualCoverage_MatchesKnownSnapshot()
    {
        var total = Matrix.Value.Components.Count;
        var realProviderCount = Matrix.Value.Components.Count(c => c.VisualCapability == "REAL_PROVIDER_VISUAL");
        var verifiedInternalCount = Matrix.Value.Components.Count(c => c.VisualCapability == "VERIFIED_INTERNAL_VISUAL");
        var fallbackCount = Matrix.Value.Components.Count(c => c.VisualCapability == "GENERIC_FALLBACK");

        Assert.Equal(71, total);
        Assert.Equal(28, realProviderCount);
        Assert.Equal(18, verifiedInternalCount);
        Assert.Equal(25, fallbackCount);
    }

    // VISUAL REFINEMENT / EXPANSION phase (Rule 3: raw component != module —
    // asserted per-entry so a future accidental identity swap can't silently
    // slip a wrong-identity visual into VERIFIED_INTERNAL_VISUAL).
    [Fact]
    public void VerifiedInternalVisual_Entries_AreNeverAssetProviderWokwiElements()
    {
        var verifiedInternal = Matrix.Value.Components.Where(c => c.VisualCapability == "VERIFIED_INTERNAL_VISUAL");
        Assert.NotEmpty(verifiedInternal);

        foreach (var entry in verifiedInternal)
        {
            // A verified-internal illustration is StemFlow's own — it must
            // never claim to BE the real @wokwi/elements asset (that's
            // REAL_PROVIDER_VISUAL's exclusive claim, see the other test).
            Assert.NotEqual("@wokwi/elements", entry.AssetProvider);
            Assert.True(entry.VisualAssetVerified);
            Assert.True(entry.PinGeometryVerified);
        }
    }

    // Relay Module and L298N: the two P0 components that needed a fresh
    // VERIFIED_INTERNAL_VISUAL judgment call this phase (STEP 3-6). Locks in
    // the specific outcome so it can't silently regress or get swapped for a
    // wrong-identity "real" source later.
    [Theory]
    [InlineData("wokwi-relay-module")]
    [InlineData("wokwi-l298n")]
    public void P0_GapComponents_NowVerifiedInternalVisual_WithCanvasWiringReady(string canonicalKey)
    {
        var entry = Matrix.Value.Components.SingleOrDefault(c => c.CanonicalKey == canonicalKey);
        Assert.NotNull(entry);
        Assert.Equal("VERIFIED_INTERNAL_VISUAL", entry!.VisualCapability);
        Assert.True(entry.CanvasWiringReady);
    }

    [Fact]
    public void Matrix_HasNoDuplicateCanonicalKeys()
    {
        var duplicates = Matrix.Value.Components
            .GroupBy(c => c.CanonicalKey)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        Assert.Empty(duplicates);
    }

    // Batch 1 (VISUAL REFINEMENT / EXPANSION, STEP 10-11): sensor/actuator
    // components whose EXISTING fallback-card SVGs were re-assessed against
    // real references and found already accurate — no new artwork needed,
    // only correcting a matrix that had never been updated to reflect it.
    [Theory]
    [InlineData("wokwi-water-leak-sensor")]
    [InlineData("wokwi-rain-sensor")]
    [InlineData("wokwi-soil-moisture-sensor")]
    [InlineData("wokwi-ir-obstacle-sensor")]
    [InlineData("wokwi-line-tracking-sensor")]
    [InlineData("wokwi-line-tracking-3ch")]
    [InlineData("wokwi-line-tracking-5ch")]
    [InlineData("wokwi-color-sensor")]
    [InlineData("wokwi-vibration-sensor")]
    [InlineData("wokwi-dc-motor")]
    [InlineData("wokwi-battery-pack")]
    [InlineData("wokwi-power-switch")]
    [InlineData("wokwi-fan")]
    [InlineData("wokwi-water-pump")]
    [InlineData("wokwi-esc")]
    [InlineData("wokwi-heating-element")]
    public void Batch1_SensorsAndActuators_AreVerifiedInternalVisual(string canonicalKey)
    {
        var entry = Matrix.Value.Components.SingleOrDefault(c => c.CanonicalKey == canonicalKey);
        Assert.NotNull(entry);
        Assert.Equal("VERIFIED_INTERNAL_VISUAL", entry!.VisualCapability);
        Assert.True(entry.VisualAssetVerified);
        Assert.True(entry.PinGeometryVerified);
    }

    // Fallback honesty (STEP 20): components with no specific real-world
    // reference to check against (generic robot-kit scene props — a "Robot
    // Wheel"/"Delivery Box"/"Ball" isn't one specific real part) correctly
    // stay GENERIC_FALLBACK rather than being force-classified upward to hit
    // a coverage number. pH Sensor stays the deliberate PIN_UNVERIFIED
    // negative-test case (identity itself unresolved, not just visual).
    [Theory]
    [InlineData("wokwi-robot-wheel")]
    [InlineData("wokwi-robot-chassis")]
    [InlineData("wokwi-breadboard")]
    [InlineData("wokwi-ph-sensor")]
    public void GenericRobotKitPropsAndUnresolvedIdentity_HonestlyStayFallback(string canonicalKey)
    {
        var entry = Matrix.Value.Components.SingleOrDefault(c => c.CanonicalKey == canonicalKey);
        Assert.NotNull(entry);
        Assert.Equal("GENERIC_FALLBACK", entry!.VisualCapability);
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
        // Polymorphic in the JSON — either a bool (false, or true after this
        // milestone's correction) or a string naming the model class (e.g.
        // "L298nModel.cs") for entries documented before boolean-only became
        // the convention. JsonElement handles both without a custom converter.
        public JsonElement QemuSupport { get; set; }
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
        public string? VisualCapability { get; set; }
    }
}
