using STEM.Application.Dtos.Simulation;

namespace STEM.Application.UseCases.Simulation.Runners.Educational.Components;

// Mirrors LedModel's digital-output shape exactly (PartId/GpioPin ->
// ToDigitalEvent) — deliberately no PWM/brightness variant (a real relay is
// a binary switch, not dimmable) and no attempt to simulate the COM/NO/NC
// contact switching itself (the runtime here has no net-propagation engine
// to actually reroute current through NO vs NC — this milestone only proves
// firmware digitalWrite(IN, ...) drives the coil state, which is the honest
// Class B bar: "runtime reacts to program logic", not "full electrical
// circuit switching").
public sealed class RelayModel
{
    public RelayModel(string partId, string gpioPin)
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
                ["component"] = "relay",
                ["state"] = value.Equals("HIGH", StringComparison.OrdinalIgnoreCase) ? "on" : "off",
                ["pin"] = GpioPin
            }
        };
    }
}
