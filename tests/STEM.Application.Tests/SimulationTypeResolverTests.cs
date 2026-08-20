using STEM.Application.UseCases.Components;

namespace STEM.Application.Tests;

public class SimulationTypeResolverTests
{
    [Fact]
    public void Resolve_LedWithCorrectPins_ReturnsWokwiLed()
    {
        Assert.Equal("wokwi-led", SimulationTypeResolver.Resolve("LED", new[] { "A", "C" }));
    }

    [Theory]
    [InlineData("BUTTON")]
    [InlineData("BUZZER")]
    public void Resolve_CategoryWithNoRequiredPins_IgnoresPinsArgument(string category)
    {
        Assert.NotNull(SimulationTypeResolver.Resolve(category, Array.Empty<string>()));
        Assert.NotNull(SimulationTypeResolver.Resolve(category, null));
    }

    // The real bug this phase's live verification caught: a Fritzing "RGB
    // LED (4 legs)" reports family="LED" (same string as a plain LED) but
    // its 4 pins (red/green/blue cathode + common anode) never normalize to
    // wokwi-led's actual "A"/"C" pins — must stay NotMapped, not silently
    // claim SIMULATION_MAPPED just because the category string matched.
    [Fact]
    public void Resolve_LedCategoryWithIncompatiblePins_ReturnsNull()
    {
        var rgbLedPins = new[] { "red cathode", "common anode", "green cathode", "blue cathode" };
        Assert.Null(SimulationTypeResolver.Resolve("LED", rgbLedPins));
    }

    [Fact]
    public void Resolve_LedCategoryWithNoPinsProvided_ReturnsNull()
    {
        Assert.Null(SimulationTypeResolver.Resolve("LED", Array.Empty<string>()));
        Assert.Null(SimulationTypeResolver.Resolve("LED", null));
    }

    [Fact]
    public void Resolve_LedCategoryWithExtraUnrelatedPinsPlusRequiredOnes_StillMatches()
    {
        // A superset is fine — the resolver only checks the required pins
        // are present, not that nothing else exists.
        Assert.Equal("wokwi-led", SimulationTypeResolver.Resolve("LED", new[] { "A", "C", "SomeExtraPin" }));
    }

    [Theory]
    [InlineData("Some Unrecognized Sensor")]
    [InlineData(null)]
    [InlineData("")]
    public void Resolve_UnknownOrMissingCategory_ReturnsNullInsteadOfGuessing(string? category)
    {
        // "NotMapped" must be a real possible outcome, never a fuzzy/best-guess
        // fallback (STEP 8: "Không đoán simulation behavior").
        Assert.Null(SimulationTypeResolver.Resolve(category, new[] { "A", "C" }));
    }
}
