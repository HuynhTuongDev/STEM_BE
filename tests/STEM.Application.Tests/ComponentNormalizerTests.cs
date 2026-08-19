using STEM.Application.UseCases.Components;
using STEM.Application.UseCases.Components.Abstractions;

namespace STEM.Application.Tests;

public class ComponentNormalizerTests
{
    private readonly ComponentNormalizer _normalizer = new();

    [Fact]
    public void Normalize_LedCandidate_MapsCathodeAndAnodeToExistingSimulationPinNames()
    {
        var candidate = new ExternalComponentCandidate(
            Provider: "fritzing",
            ExternalId: "core/LED-generic-5mm.fzp",
            Name: "Red LED - 5mm",
            Category: "LED",
            SourceUrl: "https://example.com",
            License: null,
            ExternalVersion: "4",
            Checksum: null,
            Pins: new[]
            {
                new ExternalPinCandidate("connector0", "cathode"),
                new ExternalPinCandidate("connector1", "anode")
            },
            PrimaryAssetUrl: null);

        var normalized = _normalizer.Normalize(candidate);

        Assert.Equal("led.red-led-5mm", normalized.CanonicalKeyCandidate);
        Assert.Equal("Red LED - 5mm", normalized.Name);
        Assert.Equal(2, normalized.Pins.Count);

        // "cathode"/"anode" must resolve to "C"/"A" — the exact pin names
        // wokwi-led already uses in VirtualLabDiagramService.SupportedPins,
        // so a resolved simulation mapping lines up without inventing a new
        // pin vocabulary.
        var cathode = normalized.Pins.Single(p => p.VisualPinId == "connector0");
        Assert.Equal("C", cathode.LogicalPinId);
        var anode = normalized.Pins.Single(p => p.VisualPinId == "connector1");
        Assert.Equal("A", anode.LogicalPinId);
    }

    [Theory]
    [InlineData("GND")]
    [InlineData("GROUND")]
    public void Normalize_GroundAliases_ResolveToSameLogicalPin(string rawName)
    {
        var candidate = MakeSinglePinCandidate(rawName);
        var normalized = _normalizer.Normalize(candidate);
        Assert.Equal("GND", normalized.Pins.Single().LogicalPinId);
    }

    [Theory]
    [InlineData("VCC")]
    [InlineData("5V")]
    [InlineData("POWER")]
    public void Normalize_PowerAliases_ResolveToSameLogicalPin(string rawName)
    {
        var candidate = MakeSinglePinCandidate(rawName);
        var normalized = _normalizer.Normalize(candidate);
        Assert.Equal("VCC", normalized.Pins.Single().LogicalPinId);
    }

    [Fact]
    public void Normalize_UnknownPinName_PassesThroughUnchanged()
    {
        var candidate = MakeSinglePinCandidate("SomeExoticPinName");
        var normalized = _normalizer.Normalize(candidate);
        Assert.Equal("SomeExoticPinName", normalized.Pins.Single().LogicalPinId);
    }

    private static ExternalComponentCandidate MakeSinglePinCandidate(string pinName) => new(
        Provider: "fritzing",
        ExternalId: "core/x.fzp",
        Name: "Test Part",
        Category: "Sensor",
        SourceUrl: "https://example.com",
        License: null,
        ExternalVersion: null,
        Checksum: null,
        Pins: new[] { new ExternalPinCandidate("connector0", pinName) },
        PrimaryAssetUrl: null);
}
