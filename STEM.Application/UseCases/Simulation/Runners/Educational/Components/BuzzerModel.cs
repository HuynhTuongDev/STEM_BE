using STEM.Application.Dtos.Simulation;

namespace STEM.Application.UseCases.Simulation.Runners.Educational.Components;

public sealed class BuzzerModel
{
    public BuzzerModel(string partId, string gpioPin)
    {
        PartId = partId;
        GpioPin = gpioPin;
    }

    public string PartId { get; }
    public string GpioPin { get; }

    public SimulationEventResponse ToDigitalEvent(long time, string value)
    {
        return ToStateEvent(
            time,
            value.Equals("HIGH", StringComparison.OrdinalIgnoreCase) ? "buzzing" : "silent",
            frequency: null);
    }

    public SimulationEventResponse ToToneEvent(long time, int frequency)
    {
        return ToStateEvent(time, "buzzing", frequency);
    }

    public SimulationEventResponse ToSilentEvent(long time)
    {
        return ToStateEvent(time, "silent", frequency: null);
    }

    private SimulationEventResponse ToStateEvent(long time, string state, int? frequency)
    {
        var payload = new Dictionary<string, object?>
        {
            ["partId"] = PartId,
            ["component"] = "buzzer",
            ["state"] = state,
            ["pin"] = GpioPin
        };

        if (frequency.HasValue)
        {
            payload["frequency"] = frequency.Value;
        }

        return new SimulationEventResponse
        {
            Type = "part-state",
            Time = time,
            Payload = payload
        };
    }
}
