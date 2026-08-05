using STEM.Application.Dtos.Simulation;

namespace STEM.Application.UseCases.Simulation.Runners.Educational.Components;

public sealed class LedModel
{
    public LedModel(string partId, string gpioPin)
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
                ["component"] = "led",
                ["state"] = value.Equals("HIGH", StringComparison.OrdinalIgnoreCase) ? "on" : "off",
                ["pin"] = GpioPin
            }
        };
    }

    public SimulationEventResponse ToPwmEvent(long time, int value)
    {
        return new SimulationEventResponse
        {
            Type = "part-state",
            Time = time,
            Payload = new Dictionary<string, object?>
            {
                ["partId"] = PartId,
                ["component"] = "led",
                ["state"] = value > 0 ? "on" : "off",
                ["brightness"] = Math.Clamp(value, 0, 255),
                ["pin"] = GpioPin
            }
        };
    }
}
