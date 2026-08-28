namespace STEM.Application.UseCases.Simulation.Runners.Educational.Components;

// Mirrors ButtonModel's read pattern exactly (PartId -> GpioPin -> "pin:{Gpio}"
// lookup order), but for a 12-bit ESP32 ADC value (0..4095) instead of a
// digital HIGH/LOW state.
public sealed class PotentiometerModel
{
    public const int MinValue = 0;
    public const int MaxValue = 4095;

    public PotentiometerModel(string partId, string gpioPin)
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
            // Nobody has moved the slider yet this run — same "unpressed
            // default" idea as ButtonModel, just the analog equivalent (wiper
            // at minimum) instead of LOW.
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
