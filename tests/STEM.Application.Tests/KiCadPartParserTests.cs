using STEM.Application.UseCases.Components.Providers.KiCad;

namespace STEM.Application.Tests;

public class KiCadPartParserTests
{
    // "Buzzer"/"LED" blocks captured verbatim from a real fetch of
    // .../kicad-symbols/master/Device.lib; "APA-106-F5" captured verbatim
    // from .../kicad-symbols/master/LED.lib (a *different* real file — both
    // re-verified 2026-08 after a bug this phase's own live test caught).
    // Composed into one excerpt here purely for test convenience; Parse()
    // only reads block content, never the file name, so this doesn't affect
    // what's actually under test.
    private const string DeviceLibExcerpt = """
        EESchema-LIBRARY Version 2.4
        #encoding utf-8
        #
        # Buzzer
        #
        DEF Buzzer BZ 0 1 Y N 1 F N
        F0 "BZ" 150 50 50 H V L CNN
        F1 "Buzzer" 150 -50 50 H V L CNN
        F2 "" -25 100 50 V I C CNN
        F3 "" -25 100 50 V I C CNN
        $FPLIST
         *Buzzer*
        $ENDFPLIST
        DRAW
        A 0 0 125 -899 899 0 1 0 N 0 -125 0 125
        P 2 0 1 0 -65 75 -45 75 N
        P 2 0 1 0 -55 85 -55 65 N
        P 2 0 1 0 0 125 0 -125 N
        X - 1 -100 100 100 R 50 50 1 1 P
        X + 2 -100 -100 100 R 50 50 1 1 P
        ENDDRAW
        ENDDEF
        #
        # LED
        #
        DEF LED D 0 40 N N 1 F N
        F0 "D" 0 100 50 H V C CNN
        F1 "LED" 0 -100 50 H V C CNN
        F2 "" 0 0 50 H I C CNN
        F3 "" 0 0 50 H I C CNN
        $FPLIST
         LED*
         LED_SMD:*
         LED_THT:*
        $ENDFPLIST
        DRAW
        P 2 0 1 10 -50 -50 -50 50 N
        P 2 0 1 0 -50 0 50 0 N
        P 4 0 1 10 50 -50 50 50 -50 0 50 -50 N
        X K 1 -150 0 100 R 50 50 1 1 P
        X A 2 150 0 100 L 50 50 1 1 P
        ENDDRAW
        ENDDEF
        #
        # APA-106-F5
        #
        DEF APA-106-F5 D 0 20 Y Y 1 F N
        F0 "D" 200 225 50 H V R BNN
        F1 "APA-106-F5" 50 -225 50 H V L TNN
        F2 "LED_THT:LED_D5.0mm-4_RGB" 50 -300 50 H I L TNN
        F3 "" 100 -375 50 H I L TNN
        DRAW
        X DO 1 300 100 100 L 50 50 1 1 O
        X GND 2 0 -300 100 U 50 50 1 1 W
        X VDD 3 0 300 100 D 50 50 1 1 W
        X DI 4 -300 100 100 R 50 50 1 1 I
        ENDDRAW
        ENDDEF
        #
        #End Library
        """;

    [Fact]
    public void SplitDefinitions_ReturnsAllThreeSymbols()
    {
        var definitions = KiCadPartParser.SplitDefinitions(DeviceLibExcerpt);
        Assert.Equal(3, definitions.Count);
        Assert.Contains(definitions, d => d.Name == "Buzzer");
        Assert.Contains(definitions, d => d.Name == "LED");
        Assert.Contains(definitions, d => d.Name == "APA-106-F5");
    }

    [Fact]
    public void Parse_LedSymbol_ResolvesCathodeAndAnodePinsAndKnownCategory()
    {
        var block = KiCadPartParser.SplitDefinitions(DeviceLibExcerpt).Single(d => d.Name == "LED").Block;
        var candidate = KiCadPartParser.Parse(block, "Device.lib", "https://example.com");

        Assert.NotNull(candidate);
        Assert.Equal(KiCadConstants.ProviderName, candidate!.Provider);
        Assert.Equal("Device.lib#LED", candidate.ExternalId);
        Assert.Equal("LED", candidate.Category); // exact-name allow-list hit
        Assert.Equal("CC-BY-SA-4.0", candidate.License);

        Assert.Equal(2, candidate.Pins.Count);
        Assert.Contains(candidate.Pins, p => p.VisualPinId == "1" && p.Name == "K");
        Assert.Contains(candidate.Pins, p => p.VisualPinId == "2" && p.Name == "A");
    }

    [Fact]
    public void Parse_BuzzerSymbol_ResolvesPlusMinusPinsAndCategory()
    {
        var block = KiCadPartParser.SplitDefinitions(DeviceLibExcerpt).Single(d => d.Name == "Buzzer").Block;
        var candidate = KiCadPartParser.Parse(block, "Device.lib", "https://example.com");

        Assert.NotNull(candidate);
        Assert.Equal("Buzzer", candidate!.Category);
        Assert.Contains(candidate.Pins, p => p.Name == "-");
        Assert.Contains(candidate.Pins, p => p.Name == "+");
    }

    // The critical negative case for STEP 5/10: a real 4-pin addressable
    // RGB LED (DO/GND/VDD/DI — a data protocol, not anode/cathode) living
    // in the same library file as the plain LED must NOT be categorized as
    // "LED" just by proximity/name-similarity.
    [Fact]
    public void Parse_RgbAddressableLed_DoesNotGetCategorizedAsPlainLed()
    {
        var block = KiCadPartParser.SplitDefinitions(DeviceLibExcerpt).Single(d => d.Name == "APA-106-F5").Block;
        var candidate = KiCadPartParser.Parse(block, "Device.lib", "https://example.com");

        Assert.NotNull(candidate);
        Assert.Null(candidate!.Category);
    }
}
