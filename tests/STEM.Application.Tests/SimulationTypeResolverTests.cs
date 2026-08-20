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

    // Component Compatibility Matrix milestone (STEP 4): explicit regression
    // guards for the two providers-can't-honestly-supply-this findings from
    // that audit. Live-verified (2026-08, this phase): KiCad's real
    // R_Potentiometer symbol has bare pins "1"/"2"/"3" (no VCC/SIG/GND
    // identity — a schematic rheostat, not a breakout module) and Fritzing's
    // real LDR (core/LDR_photocell_300mil.fzp) has bare pins "pin 0"/"pin 1".
    // "POTENTIOMETER"/"LDR"/"PHOTORESISTOR" are deliberately absent from
    // CategoryToRule — this test locks that omission in as intentional, so a
    // future careless addition (e.g. "category says potentiometer, map it")
    // has to deliberately change/remove this test, not silently slip past it.
    [Theory]
    [InlineData("POTENTIOMETER")]
    [InlineData("LDR")]
    [InlineData("PHOTORESISTOR")]
    public void Resolve_PassiveComponentCategoriesWithNoBreakoutModuleIdentity_NeverMapped(string category)
    {
        // Even with pins that superficially look plausible, category alone
        // must not be enough — these categories simply have no rule at all.
        Assert.Null(SimulationTypeResolver.Resolve(category, new[] { "1", "2", "3" }));
        Assert.Null(SimulationTypeResolver.Resolve(category, new[] { "SIG", "VCC", "GND" }));
        Assert.Null(SimulationTypeResolver.Resolve(category, null));
    }
}
