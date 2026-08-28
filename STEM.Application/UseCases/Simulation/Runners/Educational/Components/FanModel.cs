using STEM.Application.Dtos.Simulation;

namespace STEM.Application.UseCases.Simulation.Runners.Educational.Components;

// Mirrors RelayModel's digital-output shape exactly (PartId/GpioPin ->
// ToDigitalEvent) — deliberately binary ON/OFF only. QEMU (QemuEsp32Runner,
// where this model is actually wired — see ComponentIndex) only instruments
// digitalWrite via SF_EVENT, never analogWrite/ledcWrite, so PWM speed
// control is not observable; a real Fan/DC Fan with speed control would need
// that capability, which does not exist anywhere in this pipeline today.
public sealed class FanModel
{
    public FanModel(string partId, string gpioPin)
    {
        PartId = partId;
        GpioPin = gpioPin;
    }

    public string PartId { get; }
    public string GpioPin { get; }

    public SimulationEventResponse ToDigitalEvent(long time, string value)
    {
        return new SimulationEventResponse
        {
            Type = "part-state",
            Time = time,
            Payload = new Dictionary<string, object?>
            {
                ["partId"] = PartId,
                ["component"] = "fan",
                ["state"] = value.Equals("HIGH", StringComparison.OrdinalIgnoreCase) ? "on" : "off",
                ["pin"] = GpioPin
            }
        };
    }
}
