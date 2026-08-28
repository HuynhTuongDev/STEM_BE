using STEM.Application.UseCases.Simulation;

namespace STEM.Application.Tests;

// Component Source Resolution milestone — Soil Moisture Sensor vertical
// slice. Pin SEMANTICS (VCC/GND/DO/AO, standard YL-69 + LM393 comparator
// module) were verified this milestone via cross-corroborated vendor
// documentation (no single official manufacturer for this generic module,
// but the pin convention is consistent everywhere it's documented). Pin
// GEOMETRY (visual anchor position) is still NOT verified — no matching
// real element/CAD asset found in @wokwi/elements, Fritzing core, or KiCad
// core. A wiring rule only needs pin semantics, not geometry, so it's
// added here even though the canvas badge stays "Chưa xác minh sơ đồ
// chân" (see component-compatibility.json's canvasWiringReady=false).
public sealed class SoilMoistureSensorWiringTests
{
    private const string ValidSoilMoistureDiagram = """
    {
      "version": 1,
      "parts": [
        { "type": "board-esp32-devkit-c-v4", "id": "esp" },
        { "type": "wokwi-soil-moisture-sensor", "id": "soil1" }
      ],
      "connections": [
        [ "soil1:AO", "esp:GPIO34" ],
        [ "soil1:VCC", "esp:5V" ],
        [ "soil1:GND", "esp:GND.1" ]
      ]
    }
    """;

    [Fact]
    public void Analyze_ValidSoilMoistureWiring_IsValid()
    {
        var service = new VirtualLabDiagramService();
        var result = service.Analyze(ValidSoilMoistureDiagram);
        Assert.True(result.Validation.IsValid, string.Join("; ", result.Validation.Errors));
    }

    [Fact]
    public void Analyze_SoilMoistureAoNotConnectedToGpio_IsInvalid()
    {
        var service = new VirtualLabDiagramService();
        var diagram = """
        {
          "version": 1,
          "parts": [
            { "type": "board-esp32-devkit-c-v4", "id": "esp" },
            { "type": "wokwi-soil-moisture-sensor", "id": "soil1" }
          ],
          "connections": [
            [ "soil1:VCC", "esp:5V" ],
            [ "soil1:GND", "esp:GND.1" ]
          ]
        }
        """;

        var result = service.Analyze(diagram);
        Assert.False(result.Validation.IsValid);
        Assert.Contains(result.Validation.Errors, e => e.Contains("Soil moisture AO must reach an ESP32 GPIO"));
    }

    [Fact]
    public void Analyze_SoilMoistureMissingGround_IsInvalid()
    {
        var service = new VirtualLabDiagramService();
        var diagram = """
        {
          "version": 1,
          "parts": [
            { "type": "board-esp32-devkit-c-v4", "id": "esp" },
            { "type": "wokwi-soil-moisture-sensor", "id": "soil1" }
          ],
          "connections": [
            [ "soil1:AO", "esp:GPIO34" ],
            [ "soil1:VCC", "esp:5V" ]
          ]
        }
        """;

        var result = service.Analyze(diagram);
        Assert.False(result.Validation.IsValid);
        Assert.Contains(result.Validation.Errors, e => e.Contains("Soil moisture must connect to GND"));
    }

    [Fact]
    public void Analyze_SoilMoistureMissingPower_IsInvalid()
    {
        var service = new VirtualLabDiagramService();
        var diagram = """
        {
          "version": 1,
          "parts": [
            { "type": "board-esp32-devkit-c-v4", "id": "esp" },
            { "type": "wokwi-soil-moisture-sensor", "id": "soil1" }
          ],
          "connections": [
            [ "soil1:AO", "esp:GPIO34" ],
            [ "soil1:GND", "esp:GND.1" ]
          ]
        }
        """;

        var result = service.Analyze(diagram);
        Assert.False(result.Validation.IsValid);
        Assert.Contains(result.Validation.Errors, e => e.Contains("Soil moisture power must connect to 3V3/5V"));
    }
}
