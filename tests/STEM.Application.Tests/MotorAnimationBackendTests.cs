using STEM.Application.UseCases.Simulation;
using STEM.Application.UseCases.Simulation.Runners.Educational.Components;

namespace STEM.Application.Tests;

// TASK "STANDARDIZE MOTOR / ROTATING COMPONENT ANIMATION" (2026-08-24) — Fan
// and Drone Motor previously had no runtime signal at all (Fan: pins were
// power-terminals-only, no control pin; Drone Motor: no SupportedPins entry,
// couldn't even be wired to anything). Locks in the new FanModel.cs/
// DroneMotorModel.cs event shape (mirrors RelayModel.cs exactly — binary
// ON/OFF from digitalWrite, no PWM: QEMU cannot observe analogWrite/
// ledcWrite, see GpioInstrumentationPreamble) and the new wiring rules in
// VirtualLabDiagramService.cs. ComponentIndex itself (QemuEsp32Runner's
// private nested pin-role lookup) is not unit-testable in isolation (private,
// by design — mirrors LED/Buzzer/Relay's own untested-in-isolation status);
// the full pipeline (ComponentIndex -> FanModel/DroneMotorModel -> real
// "part-state" event) was live-verified via a real Docker/arduino-cli
// compile + QEMU run this milestone (see PR notes), same technique as the
// DHT22 LAB10 fix and Robot Delivery LAB01-08 checkpoints.
public sealed class MotorAnimationBackendTests
{
    [Theory]
    [InlineData("HIGH", "on")]
    [InlineData("LOW", "off")]
    [InlineData("high", "on")]
    [InlineData("low", "off")]
    public void FanModel_ToDigitalEvent_MapsDigitalWriteToOnOff(string rawValue, string expectedState)
    {
        var model = new FanModel("fan1", "32");

        var evt = model.ToDigitalEvent(1234, rawValue);

        Assert.Equal("part-state", evt.Type);
        Assert.Equal(1234, evt.Time);
        Assert.Equal("fan1", evt.Payload["partId"]);
        Assert.Equal("fan", evt.Payload["component"]);
        Assert.Equal(expectedState, evt.Payload["state"]);
        Assert.Equal("32", evt.Payload["pin"]);
    }

    [Theory]
    [InlineData("HIGH", "on")]
    [InlineData("LOW", "off")]
    public void DroneMotorModel_ToDigitalEvent_MapsDigitalWriteToOnOff(string rawValue, string expectedState)
    {
        var model = new DroneMotorModel("drone1", "27");

        var evt = model.ToDigitalEvent(5678, rawValue);

        Assert.Equal("part-state", evt.Type);
        Assert.Equal(5678, evt.Time);
        Assert.Equal("drone1", evt.Payload["partId"]);
        Assert.Equal("drone-motor", evt.Payload["component"]);
        Assert.Equal(expectedState, evt.Payload["state"]);
        Assert.Equal("27", evt.Payload["pin"]);
        // SIMPLIFIED_ELECTRICAL_MODEL marker (MOTOR ANIMATION VERIFICATION
        // milestone, 2026-08-24) — every emitted event must be tagged so
        // nothing downstream can mistake this for a real ESC-mediated model.
        Assert.Equal("SIMPLIFIED_ELECTRICAL_MODEL", evt.Payload["electricalModel"]);
    }

    private static readonly VirtualLabDiagramService DiagramService = new();

    [Fact]
    public void Fan_WiredInToGpio_PassesDiagramValidation()
    {
        const string diagram = """
            {
              "version": 1,
              "parts": [
                { "type": "board-esp32-devkit-c-v4", "id": "esp" },
                { "type": "wokwi-fan", "id": "fan1" }
              ],
              "connections": [
                ["esp:GPIO32", "fan1:IN"]
              ]
            }
            """;

        var analysis = DiagramService.Analyze(diagram);

        Assert.DoesNotContain(analysis.Validation.Errors, e => e.Contains("Fan IN"));
    }

    [Fact]
    public void Fan_InNotWired_FailsDiagramValidation()
    {
        const string diagram = """
            {
              "version": 1,
              "parts": [
                { "type": "board-esp32-devkit-c-v4", "id": "esp" },
                { "type": "wokwi-fan", "id": "fan1" }
              ],
              "connections": []
            }
            """;

        var analysis = DiagramService.Analyze(diagram);

        Assert.Contains(analysis.Validation.Errors, e => e.Contains("Fan IN"));
    }

    [Fact]
    public void DroneMotor_WiredInToGpio_PassesDiagramValidation()
    {
        const string diagram = """
            {
              "version": 1,
              "parts": [
                { "type": "board-esp32-devkit-c-v4", "id": "esp" },
                { "type": "wokwi-drone-motor", "id": "drone1" }
              ],
              "connections": [
                ["esp:GPIO27", "drone1:IN"]
              ]
            }
            """;

        var analysis = DiagramService.Analyze(diagram);

        Assert.DoesNotContain(analysis.Validation.Errors, e => e.Contains("Drone Motor IN"));
    }

    [Fact]
    public void DroneMotor_InNotWired_FailsDiagramValidation()
    {
        const string diagram = """
            {
              "version": 1,
              "parts": [
                { "type": "board-esp32-devkit-c-v4", "id": "esp" },
                { "type": "wokwi-drone-motor", "id": "drone1" }
              ],
              "connections": []
            }
            """;

        var analysis = DiagramService.Analyze(diagram);

        Assert.Contains(analysis.Validation.Errors, e => e.Contains("Drone Motor IN"));
    }
}
