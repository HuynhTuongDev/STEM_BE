using System.Text.Json;
using System.Text.RegularExpressions;
using STEM.Application.Dtos.Simulation;

namespace STEM.Application.UseCases.Simulation;

public class VirtualLabMockRunner
{
    private static readonly Regex DefineRegex = new(@"^\s*#define\s+(?<name>[A-Za-z_]\w*)\s+(?<value>[A-Za-z0-9_]+)", RegexOptions.Compiled);
    private static readonly Regex IntConstRegex = new(@"\b(?:const\s+)?int\s+(?<name>[A-Za-z_]\w*)\s*=\s*(?<value>\d+)\s*;", RegexOptions.Compiled);
    private static readonly Regex PinModeRegex = new(@"\bpinMode\s*\(\s*(?<pin>[^,\)]+)\s*,\s*(?<mode>INPUT_PULLUP|INPUT|OUTPUT)\s*\)", RegexOptions.Compiled);
    private static readonly Regex DigitalWriteRegex = new(@"\bdigitalWrite\s*\(\s*(?<pin>[^,\)]+)\s*,\s*(?<value>HIGH|LOW|true|false|1|0)\s*\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex DigitalReadRegex = new(@"\bdigitalRead\s*\(\s*(?<pin>[^\)]+)\s*\)", RegexOptions.Compiled);
    private static readonly Regex DelayRegex = new(@"\bdelay\s*\(\s*(?<ms>\d+)\s*\)", RegexOptions.Compiled);
    private static readonly Regex SerialRegex = new(@"\bSerial\.(?<method>begin|print|println)\s*\((?<arg>[^\)]*)\)", RegexOptions.Compiled);

    public IReadOnlyCollection<SimulationEventResponse> Run(
        string sourceCode,
        string diagramJson,
        VirtualLabDiagramAnalysis analysis)
    {
        var events = new List<SimulationEventResponse>();
        var symbols = BuildSymbolTable(sourceCode);
        var parts = ReadParts(diagramJson);
        long time = 0;

        if (!analysis.Validation.IsValid)
        {
            Add(events, "error", time, new Dictionary<string, object?>
            {
                ["message"] = "Diagram validation failed.",
                ["errors"] = analysis.Validation.Errors
            });
        }

        if (string.IsNullOrWhiteSpace(sourceCode))
        {
            Add(events, "error", time, new Dictionary<string, object?>
            {
                ["message"] = "sourceCode is required."
            });
            return events;
        }

        Add(events, "serial", time, new Dictionary<string, object?>
        {
            ["message"] = "StemFlow mock runner started."
        });

        foreach (var rawLine in sourceCode.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
        {
            var line = StripLineComment(rawLine).Trim();
            if (line.Length == 0)
            {
                continue;
            }

            foreach (Match match in SerialRegex.Matches(line))
            {
                var method = match.Groups["method"].Value;
                if (method.Equals("begin", StringComparison.OrdinalIgnoreCase))
                {
                    Add(events, "serial", time, new Dictionary<string, object?>
                    {
                        ["message"] = $"Serial.begin({match.Groups["arg"].Value.Trim()})"
                    });
                    continue;
                }

                Add(events, "serial", time, new Dictionary<string, object?>
                {
                    ["message"] = ExtractSerialMessage(match.Groups["arg"].Value),
                    ["newline"] = method.Equals("println", StringComparison.OrdinalIgnoreCase)
                });
            }

            foreach (Match match in PinModeRegex.Matches(line))
            {
                Add(events, "pin-state", time, new Dictionary<string, object?>
                {
                    ["pin"] = Resolve(match.Groups["pin"].Value, symbols),
                    ["mode"] = match.Groups["mode"].Value
                });
            }

            foreach (Match match in DigitalReadRegex.Matches(line))
            {
                Add(events, "pin-state", time, new Dictionary<string, object?>
                {
                    ["pin"] = Resolve(match.Groups["pin"].Value, symbols),
                    ["value"] = "LOW",
                    ["operation"] = "digitalRead"
                });
            }

            foreach (Match match in DigitalWriteRegex.Matches(line))
            {
                var value = NormalizeDigitalValue(match.Groups["value"].Value);
                var pin = Resolve(match.Groups["pin"].Value, symbols);
                Add(events, "pin-state", time, new Dictionary<string, object?>
                {
                    ["pin"] = pin,
                    ["value"] = value,
                    ["operation"] = "digitalWrite"
                });

                AddPartStateEvents(events, time, parts, value, pin);
            }

            var delayMatch = DelayRegex.Match(line);
            if (delayMatch.Success && long.TryParse(delayMatch.Groups["ms"].Value, out var delayMs))
            {
                time += delayMs;
            }
        }

        if (events.Count == 1)
        {
            Add(events, "part-state", time, new Dictionary<string, object?>
            {
                ["state"] = "idle",
                ["message"] = "No supported Arduino IO calls were detected."
            });
        }

        return events;
    }

    private static void AddPartStateEvents(
        ICollection<SimulationEventResponse> events,
        long time,
        IReadOnlyCollection<DiagramPartSnapshot> parts,
        string value,
        string pin)
    {
        var state = value.Equals("HIGH", StringComparison.OrdinalIgnoreCase) ? "on" : "off";
        foreach (var part in parts)
        {
            if (part.Type.Equals("wokwi-led", StringComparison.OrdinalIgnoreCase))
            {
                Add(events, "part-state", time, new Dictionary<string, object?>
                {
                    ["partId"] = part.Id,
                    ["component"] = "led",
                    ["state"] = state,
                    ["pin"] = pin
                });
            }
            else if (part.Type.Equals("wokwi-buzzer", StringComparison.OrdinalIgnoreCase))
            {
                Add(events, "part-state", time, new Dictionary<string, object?>
                {
                    ["partId"] = part.Id,
                    ["component"] = "buzzer",
                    ["state"] = state.Equals("on", StringComparison.OrdinalIgnoreCase) ? "buzzing" : "silent",
                    ["pin"] = pin
                });
            }
        }
    }

    private static Dictionary<string, string> BuildSymbolTable(string sourceCode)
    {
        var symbols = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in sourceCode.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
        {
            var define = DefineRegex.Match(line);
            if (define.Success)
            {
                symbols[define.Groups["name"].Value] = define.Groups["value"].Value;
            }

            var intConst = IntConstRegex.Match(line);
            if (intConst.Success)
            {
                symbols[intConst.Groups["name"].Value] = intConst.Groups["value"].Value;
            }
        }

        return symbols;
    }

    private static IReadOnlyCollection<DiagramPartSnapshot> ReadParts(string diagramJson)
    {
        var parts = new List<DiagramPartSnapshot>();
        if (string.IsNullOrWhiteSpace(diagramJson))
        {
            return parts;
        }

        try
        {
            using var document = JsonDocument.Parse(diagramJson);
            if (!document.RootElement.TryGetProperty("parts", out var partsElement) ||
                partsElement.ValueKind != JsonValueKind.Array)
            {
                return parts;
            }

            foreach (var part in partsElement.EnumerateArray())
            {
                if (!part.TryGetProperty("id", out var idProperty) ||
                    !part.TryGetProperty("type", out var typeProperty))
                {
                    continue;
                }

                var id = idProperty.GetString();
                var type = typeProperty.GetString();
                if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(type))
                {
                    parts.Add(new DiagramPartSnapshot(id, type));
                }
            }
        }
        catch (JsonException)
        {
        }

        return parts;
    }

    private static string StripLineComment(string line)
    {
        var index = line.IndexOf("//", StringComparison.Ordinal);
        return index >= 0 ? line[..index] : line;
    }

    private static string Resolve(string rawPin, IReadOnlyDictionary<string, string> symbols)
    {
        var pin = rawPin.Trim();
        return symbols.TryGetValue(pin, out var value) ? value : pin;
    }

    private static string NormalizeDigitalValue(string value)
    {
        return value.Equals("HIGH", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
               value == "1"
            ? "HIGH"
            : "LOW";
    }

    private static string ExtractSerialMessage(string argument)
    {
        var trimmed = argument.Trim();
        if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[^1] == '"')
        {
            return trimmed[1..^1];
        }

        return string.IsNullOrWhiteSpace(trimmed) ? string.Empty : $"<{trimmed}>";
    }

    private static void Add(
        ICollection<SimulationEventResponse> events,
        string type,
        long time,
        IReadOnlyDictionary<string, object?> payload)
    {
        events.Add(new SimulationEventResponse
        {
            Type = type,
            Time = time,
            Payload = payload
        });
    }

    private sealed record DiagramPartSnapshot(string Id, string Type);
}
