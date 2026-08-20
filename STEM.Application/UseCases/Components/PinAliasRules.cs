namespace STEM.Application.UseCases.Components;

// Extensible alias -> logical pin id rule layer (STEP 14 of the Multi-Provider
// architecture). Deliberately a flat, editable table — not hard-coded in a
// controller/handler — so adding a new provider's naming quirks later is a
// data change here, not a code change at every call site. Covers both
// generic electrical roles (power/ground/trigger/data) and the specific
// logical pin names existing wokwi-* SupportedPins/simulation runners
// already use (e.g. LED "A"/"C"), so a normalized pin can line up with the
// current runtime vocabulary without inventing a new one.
public static class PinAliasRules
{
    private static readonly IReadOnlyDictionary<string, string> Aliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Power
            ["VCC"] = "VCC",
            ["5V"] = "VCC",
            ["POWER"] = "VCC",
            ["VDD"] = "VCC",
            ["V+"] = "VCC",
            ["3V3"] = "VCC",
            ["3.3V"] = "VCC",
            ["POS"] = "VCC",
            ["+"] = "VCC",

            // Ground
            ["GND"] = "GND",
            ["GROUND"] = "GND",
            ["VSS"] = "GND",
            ["NEG"] = "GND",
            ["-"] = "GND",

            // Signal
            ["TRIG"] = "TRIG",
            ["TRIGGER"] = "TRIG",
            ["ECHO"] = "ECHO",
            ["DIN"] = "DIN",
            ["DATA_IN"] = "DIN",
            ["SI"] = "DIN",
            ["DOUT"] = "DOUT",
            ["DATA_OUT"] = "DOUT",
            ["SO"] = "DOUT",
            ["PWM"] = "PWM",
            ["SIG"] = "SIG",
            ["SIGNAL"] = "SIG",

            // LED — matches wokwi-led's existing SupportedPins ("A", "C").
            // "K" = Kathode, the KiCad/EE convention for cathode (verified
            // against the real KiCad Device.lib "LED" symbol: X K 1 ...).
            ["ANODE"] = "A",
            ["A"] = "A",
            ["CATHODE"] = "C",
            ["KATHODE"] = "C",
            ["K"] = "C",
            ["C"] = "C",

            // Buzzer / generic 2-pin actuator
            ["1"] = "1",
            ["2"] = "2",

            // Push button — matches wokwi-pushbutton's existing SupportedPins
            ["1.L"] = "1.l",
            ["2.L"] = "2.l",
            ["1.R"] = "1.r",
            ["2.R"] = "2.r",
            ["NO"] = "1.r",
            ["COM"] = "1.l",

            // Servo — matches wokwi-servo's existing SupportedPins
            ["PULSE"] = "PWM",
        };

    public static string Resolve(string rawPinName)
    {
        var trimmed = rawPinName.Trim();
        return Aliases.TryGetValue(trimmed, out var logicalId) ? logicalId : trimmed;
    }
}
