namespace STEM.Application.UseCases.Simulation.Runners.Educational.Components;

public sealed class ButtonModel
{
    public ButtonModel(string partId, string gpioPin)
    {
        PartId = partId;
        GpioPin = gpioPin;
    }

    public string PartId { get; }
    public string GpioPin { get; }

    public string Read(
        IReadOnlyDictionary<string, object> componentInputs,
        string? pinMode)
    {
        var defaultValue = pinMode?.Equals("INPUT_PULLUP", StringComparison.OrdinalIgnoreCase) == true
            ? "HIGH"
            : "LOW";

        if (!TryReadInput(componentInputs, PartId, out var value) &&
            !TryReadInput(componentInputs, GpioPin, out value) &&
            !TryReadInput(componentInputs, $"pin:{GpioPin}", out value))
        {
            return defaultValue;
        }

        return NormalizeInput(value, defaultValue);
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

    private static string NormalizeInput(object value, string defaultValue)
    {
        return value switch
        {
            bool pressed => pressed ? "HIGH" : "LOW",
            int number => number == 0 ? "LOW" : "HIGH",
            long number => number == 0 ? "LOW" : "HIGH",
            string text when text.Equals("HIGH", StringComparison.OrdinalIgnoreCase) => "HIGH",
            string text when text.Equals("LOW", StringComparison.OrdinalIgnoreCase) => "LOW",
            string text when text.Equals("true", StringComparison.OrdinalIgnoreCase) => "HIGH",
            string text when text.Equals("false", StringComparison.OrdinalIgnoreCase) => "LOW",
            string text when text.Equals("pressed", StringComparison.OrdinalIgnoreCase) => "HIGH",
            string text when text.Equals("released", StringComparison.OrdinalIgnoreCase) => defaultValue,
            _ => defaultValue
        };
    }
}
