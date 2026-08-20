namespace STEM.Application.UseCases.Simulation.Runners.Educational.Components;

// Structurally identical to PotentiometerModel (12-bit ESP32 ADC value via
// analogRead) — kept as its own class rather than a shared base, matching
// this codebase's existing style (LedModel/BuzzerModel are separate classes
// too despite similar shapes). Distinct class = distinct SensorKind at the
// SimulationInputEvent level without any generic "sensor base type" to design
// around before there's a second real sensor that needs it.
public sealed class LightSensorModel
{
    public const int MinValue = 0;
    public const int MaxValue = 4095;

    public LightSensorModel(string partId, string gpioPin)
    {
        PartId = partId;
        GpioPin = gpioPin;
    }

    public string PartId { get; }
    public string GpioPin { get; }

    public int Read(IReadOnlyDictionary<string, object> componentInputs)
    {
        if (!TryReadInput(componentInputs, PartId, out var value) &&
            !TryReadInput(componentInputs, GpioPin, out value) &&
            !TryReadInput(componentInputs, $"pin:{GpioPin}", out value))
        {
            return MinValue;
        }

        return NormalizeInput(value);
    }

    private static bool TryReadInput(
        IReadOnlyDictionary<string, object> componentInputs,
        string key,
        out object value)
    {
        foreach (var item in componentInputs)
        {
            if (item.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                value = item.Value;
                return true;
            }
        }

        value = string.Empty;
        return false;
    }

    private static int NormalizeInput(object value)
    {
        var parsed = value switch
        {
            int i => i,
            long l => (int)l,
            double d => (int)d,
            string s when int.TryParse(s, out var fromString) => fromString,
            _ => MinValue
        };

        return Math.Clamp(parsed, MinValue, MaxValue);
    }
}
