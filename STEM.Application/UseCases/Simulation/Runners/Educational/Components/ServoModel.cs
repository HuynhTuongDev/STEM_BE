using STEM.Application.Dtos.Simulation;

namespace STEM.Application.UseCases.Simulation.Runners.Educational.Components;

public sealed class ServoModel
{
    public ServoModel(string partId, string gpioPin)
    {
        PartId = partId;
        GpioPin = gpioPin;
    }

    public string PartId { get; }
    public string GpioPin { get; }

    public SimulationEventResponse ToAngleEvent(long time, int angle)
    {
        return new SimulationEventResponse
        {
            Type = "part-state",
            Time = time,
            Payload = new Dictionary<string, object?>
            {
                ["partId"] = PartId,
                ["component"] = "servo",
                ["state"] = "angle",
                ["angle"] = Math.Clamp(angle, 0, 180),
                ["pin"] = GpioPin
            }
        };
    }
}
