using STEM.Application.UseCases.Simulation;

namespace STEM.Application.Tests;

// Verified External Component Assets milestone — PIR Motion Sensor vertical
// slice. The visual + pin geometry were already real (wokwi-pir-motion-sensor
// element, @wokwi/elements, MIT license) before this milestone; the gap
// closed here is the missing dedicated wiring rule (previously fell to the
// generic structural-only catch-all despite having real pin identity).
public sealed class PirMotionSensorWiringTests
{
    private const string ValidPirDiagram = """
    {
      "version": 1,
      "parts": [
        { "type": "board-esp32-devkit-c-v4", "id": "esp" },
        { "type": "wokwi-pir-motion-sensor", "id": "pir1" }
      ],
      "connections": [
        [ "pir1:OUT", "esp:GPIO27" ],
        [ "pir1:VCC", "esp:5V" ],
        [ "pir1:GND", "esp:GND.1" ]
      ]
    }
    """;

    [Fact]
    public void Analyze_ValidPirWiring_IsValid()
    {
        var service = new VirtualLabDiagramService();
        var result = service.Analyze(ValidPirDiagram);
        Assert.True(result.Validation.IsValid, string.Join("; ", result.Validation.Errors));
    }

    [Fact]
    public void Analyze_PirOutNotConnectedToGpio_IsInvalid()
    {
        var service = new VirtualLabDiagramService();
        var diagram = """
        {
          "version": 1,
          "parts": [
            { "type": "board-esp32-devkit-c-v4", "id": "esp" },
            { "type": "wokwi-pir-motion-sensor", "id": "pir1" }
          ],
          "connections": [
            [ "pir1:VCC", "esp:5V" ],
            [ "pir1:GND", "esp:GND.1" ]
          ]
        }
        """;

        var result = service.Analyze(diagram);
        Assert.False(result.Validation.IsValid);
        Assert.Contains(result.Validation.Errors, e => e.Contains("PIR OUT must reach an ESP32 GPIO"));
    }

    [Fact]
    public void Analyze_PirMissingGround_IsInvalid()
    {
        var service = new VirtualLabDiagramService();
        var diagram = """
        {
          "version": 1,
          "parts": [
            { "type": "board-esp32-devkit-c-v4", "id": "esp" },
            { "type": "wokwi-pir-motion-sensor", "id": "pir1" }
          ],
          "connections": [
            [ "pir1:OUT", "esp:GPIO27" ],
            [ "pir1:VCC", "esp:5V" ]
          ]
        }
        """;

        var result = service.Analyze(diagram);
        Assert.False(result.Validation.IsValid);
        Assert.Contains(result.Validation.Errors, e => e.Contains("PIR must connect to GND"));
    }

    [Fact]
    public void Analyze_PirMissingPower_IsInvalid()
    {
        var service = new VirtualLabDiagramService();
        var diagram = """
        {
          "version": 1,
          "parts": [
            { "type": "board-esp32-devkit-c-v4", "id": "esp" },
            { "type": "wokwi-pir-motion-sensor", "id": "pir1" }
          ],
          "connections": [
            [ "pir1:OUT", "esp:GPIO27" ],
            [ "pir1:GND", "esp:GND.1" ]
          ]
        }
        """;

        var result = service.Analyze(diagram);
        Assert.False(result.Validation.IsValid);
        Assert.Contains(result.Validation.Errors, e => e.Contains("PIR power must connect to 3V3/5V"));
    }
}
