namespace STEM.Application.UseCases.Simulation.Runners.Educational.Components;

// Generic model for the digital-scripted-sensor family (PIR Motion Sensor;
// Water Leak/Flame/Soil Moisture/Rain/Vibration/IR Obstacle, which all share
// a single Detected field — see SensorTimelineEntry). Deliberately holds no
// runtime state of its own: the actual value always comes from
// EducationalRunState.ReadDigitalSensorScenario(PartId, ...), this class
// only records WHICH component/pin/field a given digitalRead(pin) call
// resolves to. One class covers all 6 types instead of 6 near-identical
// ones — matches STEP 11's "don't over-engineer" instruction, since the
// semantic really is identical: a scripted boolean value looked up by
// component id and read via digitalRead().
public sealed class DigitalSensorModel
{
    public DigitalSensorModel(string partId, string gpioPin, bool useMotionField)
    {
        PartId = partId;
        GpioPin = gpioPin;
        UseMotionField = useMotionField;
    }

    public string PartId { get; }
    public string GpioPin { get; }

    // true for PIR (reads SensorTimelineEntry.Motion), false for the 5
    // generic Detected-field sensors + IR Obstacle.
    public bool UseMotionField { get; }

    // Same ComponentInputs/ISimulationInputChannel lookup ButtonModel.Read
    // already uses (ComponentInputs doesn't care what kind of component is
    // on a pin, only that something wrote a value there) — so if a live FE
    // control for one of these sensors is ever built, it works with zero
    // further backend change. Returns null (not a default value) when no
    // live input exists, so the caller correctly falls back to the scripted
    // scenario timeline instead of silently treating "no live control yet"
    // as "sensor reads false forever".
    public bool? TryReadLiveInput(IReadOnlyDictionary<string, object> componentInputs)
    {
        if (TryReadInput(componentInputs, PartId, out var value) ||
            TryReadInput(componentInputs, GpioPin, out value) ||
            TryReadInput(componentInputs, $"pin:{GpioPin}", out value))
        {
            return NormalizeToBool(value);
        }

        return null;
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

    private static bool NormalizeToBool(object value)
    {
        return value switch
        {
            bool detected => detected,
            int number => number != 0,
            long number => number != 0,
            string text when text.Equals("HIGH", StringComparison.OrdinalIgnoreCase) => true,
            string text when text.Equals("true", StringComparison.OrdinalIgnoreCase) => true,
            string text when text.Equals("detected", StringComparison.OrdinalIgnoreCase) => true,
            _ => false
        };
    }
}
