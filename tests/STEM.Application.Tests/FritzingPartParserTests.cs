using STEM.Application.UseCases.Components.Providers.Fritzing;

namespace STEM.Application.Tests;

public class FritzingPartParserTests
{
    // Captured verbatim from
    // https://raw.githubusercontent.com/fritzing/fritzing-parts/master/core/LED-generic-5mm.fzp
    // (2026-08 audit) — a real Fritzing part file, not a hand-written fixture.
    private const string RealLedFzp = """
        <?xml version="1.0" encoding="UTF-8"?><module fritzingVersion="0.1.beta.1396" moduleId="5mmColorLEDModuleID">
         <version>4</version>
         <author>Brendan Howell</author>
         <title>Red LED - 5mm</title>
         <label>LED</label>
         <date>2008-07-25</date>
         <tags>
          <tag>LED</tag>
          <tag>Red LED</tag>
          <tag>indicator</tag>
          <tag>fritzing core</tag>
         </tags>
         <properties>
          <property name="package">5 mm [THT]</property>
          <property name="family">LED</property>
          <property name="color" showInLabel="yes">Red (633nm)</property>
          <property name="current" showInLabel="yes"></property>
          <property name="leg" >yes</property>
         </properties>
         <description>A generic red LED (~1.8V)</description>
         <views>
          <iconView>
           <layers image="icon/LED-red-5mmicon.svg">
            <layer layerId="icon"/>
           </layers>
          </iconView>
          <breadboardView fliphorizontal="true" flipvertical="true">
           <layers image="breadboard/LED-5mm-red-leg.svg">
            <layer layerId="breadboard"/>
           </layers>
          </breadboardView>
         </views>
         <connectors>
          <connector id="connector0" name="cathode" type="male">
           <description>cathode pin</description>
           <views>
            <breadboardView>
             <p layer="breadboard" svgId="connector0pin"  legId="connector0leg"/>
            </breadboardView>
            <schematicView>
             <p layer="schematic" svgId="connector0pin" terminalId="connector0terminal"/>
            </schematicView>
           </views>
          </connector>
          <connector id="connector1" name="anode" type="male">
           <description>anode pin</description>
           <views>
            <breadboardView>
             <p layer="breadboard" svgId="connector1pin"  legId="connector1leg"/>
            </breadboardView>
            <schematicView>
             <p layer="schematic" svgId="connector1pin" terminalId="connector1terminal"/>
            </schematicView>
           </views>
          </connector>
         </connectors>
        </module>
        """;

    [Fact]
    public void Parse_RealLedFzp_ReturnsExpectedMetadataAndPins()
    {
        var candidate = FritzingPartParser.Parse(
            RealLedFzp,
            externalId: "core/LED-generic-5mm.fzp",
            sourceUrl: "https://github.com/fritzing/fritzing-parts/blob/master/core/LED-generic-5mm.fzp",
            primaryAssetUrl: null);

        Assert.NotNull(candidate);
        Assert.Equal(FritzingConstants.ProviderName, candidate!.Provider);
        Assert.Equal("Red LED - 5mm", candidate.Name);
        Assert.Equal("LED", candidate.Category);
        Assert.Equal("core/LED-generic-5mm.fzp", candidate.ExternalId);
        Assert.Null(candidate.License); // .fzp carries no per-part license — Unknown by default (STEP 28)

        Assert.Equal(2, candidate.Pins.Count);
        Assert.Contains(candidate.Pins, p => p.VisualPinId == "connector0" && p.Name == "cathode");
        Assert.Contains(candidate.Pins, p => p.VisualPinId == "connector1" && p.Name == "anode");

        // The visual pin id must never be the raw SVG element id
        // ("connector0pin"/"connector0terminal") — those stay inside the
        // provider's own data and are never surfaced as simulation identity.
        Assert.DoesNotContain(candidate.Pins, p => p.VisualPinId.EndsWith("pin") || p.VisualPinId.EndsWith("terminal"));
    }

    [Fact]
    public void Parse_InvalidXml_ReturnsNullInsteadOfThrowing()
    {
        var candidate = FritzingPartParser.Parse(
            "<not-even-close-to-xml",
            externalId: "core/broken.fzp",
            sourceUrl: "https://example.com",
            primaryAssetUrl: null);

        Assert.Null(candidate);
    }

    [Fact]
    public void Parse_EmptyString_ReturnsNull()
    {
        var candidate = FritzingPartParser.Parse(string.Empty, "id", "url", null);
        Assert.Null(candidate);
    }

    [Fact]
    public void Parse_XmlWithoutModuleRoot_ReturnsNull()
    {
        var candidate = FritzingPartParser.Parse("<somethingElse/>", "id", "url", null);
        Assert.Null(candidate);
    }

    [Fact]
    public void Parse_ModuleWithoutTitle_ReturnsNull()
    {
        var candidate = FritzingPartParser.Parse(
            "<module moduleId=\"x\"><connectors/></module>",
            "id",
            "url",
            null);

        Assert.Null(candidate);
    }
}
