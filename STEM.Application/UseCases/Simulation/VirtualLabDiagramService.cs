using System.Text.Json;
using System.Text.Json.Nodes;
using STEM.Application.Dtos.Simulation;

namespace STEM.Application.UseCases.Simulation;

public class VirtualLabDiagramService
{
    // CircuitCanvas.tsx (FE) never puts the board into parts[] — it renders
    // the board as a fixed, separately-tracked slot with this literal id and
    // serializes it nowhere. Connections still reference "arduino:pin" though,
    // so this service treats "arduino" as the reserved board part id and
    // synthesizes a DiagramPart for it here (validation-only, never persisted
    // back into parts[]) using whichever board type is known — either the
    // diagram's own top-level "board" field or a caller-supplied fallback
    // (VirtualLabProject.Board). See VIRTUAL_LAB_PLAN.md backlog note.
    private const string BoardPartId = "arduino";

    private static readonly IReadOnlySet<string> Esp32Types = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "board-esp32-devkit-c-v4",
        "wokwi-esp32",
        "esp32-devkit-v1",
        "esp32-devkit"
    };

    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> SupportedPins =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["board-esp32-devkit-c-v4"] = PinSet(
                "3V3", "5V", "VIN", "GND", "GND.1", "GND.2", "EN", "VP", "VN", "TX0", "RX0",
                "GPIO0", "GPIO2", "GPIO4", "GPIO5", "GPIO12", "GPIO13", "GPIO14", "GPIO15",
                "GPIO16", "GPIO17", "GPIO18", "GPIO19", "GPIO21", "GPIO22", "GPIO23",
                "GPIO25", "GPIO26", "GPIO27", "GPIO32", "GPIO33", "GPIO34", "GPIO35"),
            ["wokwi-esp32"] = PinSet(
                "3V3", "5V", "VIN", "GND", "GND.1", "GND.2", "EN", "VP", "VN", "TX0", "RX0",
                "GPIO0", "GPIO2", "GPIO4", "GPIO5", "GPIO12", "GPIO13", "GPIO14", "GPIO15",
                "GPIO16", "GPIO17", "GPIO18", "GPIO19", "GPIO21", "GPIO22", "GPIO23",
                "GPIO25", "GPIO26", "GPIO27", "GPIO32", "GPIO33", "GPIO34", "GPIO35"),
            ["wokwi-led"] = PinSet("A", "C"),
            ["wokwi-resistor"] = PinSet("1", "2"),
            ["wokwi-pushbutton"] = PinSet("1.l", "2.l", "1.r", "2.r"),
            ["wokwi-buzzer"] = PinSet("1", "2"),
            ["wokwi-servo"] = PinSet("GND", "V+", "PWM"),
            ["wokwi-dht22"] = PinSet("VCC", "SDA", "NC", "GND"),
            ["wokwi-dht11"] = PinSet("VCC", "SDA", "NC", "GND"),
            ["wokwi-hc-sr04"] = PinSet("VCC", "TRIG", "ECHO", "GND"),
            ["wokwi-lcd1602"] = PinSet("VSS", "VDD", "V0", "RS", "RW", "E", "D0", "D1", "D2", "D3", "D4", "D5", "D6", "D7", "A", "K"),
            ["wokwi-lcd2004"] = PinSet("VCC", "GND", "SDA", "SCL", "A", "K"),
            ["wokwi-gnd"] = PinSet("GND"),
            ["wokwi-5v"] = PinSet("5V")
        };

    public VirtualLabDiagramAnalysis Analyze(string diagramJson, string? fallbackBoardType = null)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var parts = new List<DiagramPart>();
        var wires = new List<Wire>();
        var normalizedJson = string.IsNullOrWhiteSpace(diagramJson) ? "{}" : diagramJson.Trim();

        if (string.IsNullOrWhiteSpace(diagramJson))
        {
            errors.Add("diagramJson is required.");
            return BuildAnalysis(normalizedJson, parts, wires, errors, warnings);
        }

        try
        {
            using var document = JsonDocument.Parse(diagramJson);
            var root = document.RootElement;
            normalizedJson = root.GetRawText();
            ParseParts(root, parts, errors, warnings);

            var embeddedBoard = ReadOptionalString(root, "board");
            var effectiveBoard = embeddedBoard ?? fallbackBoardType;
            EnsureBoardPart(parts, effectiveBoard);
            if (string.IsNullOrWhiteSpace(embeddedBoard) && !string.IsNullOrWhiteSpace(effectiveBoard))
            {
                // Client never sends "board" inside the diagram payload (only
                // VirtualLabProject.Board carries it) — bake the resolved value
                // back into the JSON we persist/thread downstream, so runners
                // (VirtualLabMockRunner/EducationalSimulationRunner) that only
                // see this JSON string later don't need their own fallback.
                normalizedJson = TryEmbedBoardField(normalizedJson, effectiveBoard!) ?? normalizedJson;
            }

            ParseConnections(root, parts, wires, errors);
        }
        catch (JsonException ex)
        {
            errors.Add($"diagramJson is not valid JSON: {ex.Message}");
        }

        return BuildAnalysis(normalizedJson, parts, wires, errors, warnings);
    }

    public VirtualLabRuntimeDiagramSnapshot BuildRuntimeSnapshot(string diagramJson, string? fallbackBoardType = null)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var parts = new List<DiagramPart>();
        var wires = new List<Wire>();

        if (string.IsNullOrWhiteSpace(diagramJson))
        {
            return new VirtualLabRuntimeDiagramSnapshot(Array.Empty<VirtualLabRuntimeComponent>());
        }

        try
        {
            using var document = JsonDocument.Parse(diagramJson);
            var root = document.RootElement;
            ParseParts(root, parts, errors, warnings);
            EnsureBoardPart(parts, ReadOptionalString(root, "board") ?? fallbackBoardType);
            ParseConnections(root, parts, wires, errors);
        }
        catch (JsonException)
        {
            return new VirtualLabRuntimeDiagramSnapshot(Array.Empty<VirtualLabRuntimeComponent>());
        }

        var netlist = BuildValidationConnectivity(parts, wires);
        var components = BuildRuntimeComponents(parts, netlist);
        return new VirtualLabRuntimeDiagramSnapshot(components);
    }

    // Reserved board part id is never present in parts[] as sent by the FE
    // (see BoardPartId comment above) — add a synthetic entry so connection
    // validation (ValidatePinReference) and GPIO-reachability checks
    // (IsBoardGpio) can resolve "arduino:pin" references. Never mutates an
    // existing real part with that id, and does nothing when no board type
    // is known at all (preserves prior behavior for callers with no context).
    private static void EnsureBoardPart(ICollection<DiagramPart> parts, string? effectiveBoard)
    {
        if (string.IsNullOrWhiteSpace(effectiveBoard))
        {
            return;
        }

        if (parts.Any(part => part.Id.Equals(BoardPartId, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        parts.Add(new DiagramPart(BoardPartId, NormalizeBoardPartType(effectiveBoard)));
    }

    // Board vocabulary is inconsistent across the codebase (Lab.BoardType:
    // "esp32_devkit_v1"/"arduino_uno"; VirtualLabProject.Board: "esp32" or
    // compile FQBNs like "esp32:esp32:esp32"/"arduino:avr:uno"; diagram JSON
    // "board": "esp32_devkit_v1"/"arduino_uno"). Match loosely on "esp32"
    // rather than depend on any one convention. Non-ESP32 boards (Arduino
    // Uno) map to a type outside both Esp32Types and SupportedPins, so pin
    // validation is skipped for them (unmodeled board, same as other
    // unmodeled component types) and "Diagram must include an ESP32 board."
    // still fires — intentional for the 2 Arduino Uno labs kept failing.
    private static string NormalizeBoardPartType(string board)
    {
        return board.Contains("esp32", StringComparison.OrdinalIgnoreCase)
            ? "board-esp32-devkit-c-v4"
            : "wokwi-arduino-uno";
    }

    private static string? ReadOptionalString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;
    }

    private static string? TryEmbedBoardField(string json, string board)
    {
        if (JsonNode.Parse(json) is not JsonObject node)
        {
            return null;
        }

        node["board"] = board;
        return node.ToJsonString();
    }

    private static VirtualLabDiagramAnalysis BuildAnalysis(
        string diagramJson,
        IReadOnlyCollection<DiagramPart> parts,
        IReadOnlyCollection<Wire> wires,
        List<string> errors,
        List<string> warnings)
    {
        if (parts.Count > 0 && !parts.Any(part => Esp32Types.Contains(part.Type)))
        {
            errors.Add("Diagram must include an ESP32 board.");
        }

        var netlist = BuildNetlist(wires);
        var validationNetlist = BuildValidationConnectivity(parts, wires);
        ValidateComponentWiring(parts, validationNetlist.PinToNet, errors, warnings);

        return new VirtualLabDiagramAnalysis(
            diagramJson,
            new DiagramValidationResponse
            {
                IsValid = errors.Count == 0,
                Errors = errors,
                Warnings = warnings
            },
            netlist);
    }

    private static void ParseParts(
        JsonElement root,
        ICollection<DiagramPart> parts,
        ICollection<string> errors,
        ICollection<string> warnings)
    {
        if (!root.TryGetProperty("parts", out var partsElement) || partsElement.ValueKind != JsonValueKind.Array)
        {
            errors.Add("parts is required and must be an array.");
            return;
        }

        var partIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var partElement in partsElement.EnumerateArray())
        {
            var id = ReadString(partElement, "id");
            var type = ReadString(partElement, "type");

            if (string.IsNullOrWhiteSpace(id))
            {
                errors.Add("A part is missing id.");
                continue;
            }

            if (!partIds.Add(id))
            {
                errors.Add($"Duplicate part id: {id}.");
            }

            if (string.IsNullOrWhiteSpace(type))
            {
                errors.Add($"{id}: missing type.");
                continue;
            }

            if (!SupportedPins.ContainsKey(type))
            {
                warnings.Add($"{id}: component type '{type}' is not modeled by the MVP validator.");
            }

            parts.Add(new DiagramPart(id, type));
        }
    }

    private static void ParseConnections(
        JsonElement root,
        IReadOnlyCollection<DiagramPart> parts,
        ICollection<Wire> wires,
        ICollection<string> errors)
    {
        if (!root.TryGetProperty("connections", out var connectionsElement))
        {
            return;
        }

        if (connectionsElement.ValueKind != JsonValueKind.Array)
        {
            errors.Add("connections must be an array.");
            return;
        }

        var partsById = parts
            .GroupBy(part => part.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var connection in connectionsElement.EnumerateArray())
        {
            if (connection.ValueKind != JsonValueKind.Array || connection.GetArrayLength() < 2)
            {
                errors.Add("Each connection must be an array with at least two pin references.");
                continue;
            }

            var source = connection[0].GetString();
            var target = connection[1].GetString();
            if (!TryParsePinToken(source, out var sourcePartId, out var sourcePin) ||
                !TryParsePinToken(target, out var targetPartId, out var targetPin))
            {
                errors.Add($"Invalid connection pin reference: {source} -> {target}.");
                continue;
            }

            ValidatePinReference(partsById, sourcePartId, sourcePin, errors);
            ValidatePinReference(partsById, targetPartId, targetPin, errors);

            var a = ToPinToken(sourcePartId, sourcePin);
            var b = ToPinToken(targetPartId, targetPin);
            var duplicateKey = string.Compare(a, b, StringComparison.OrdinalIgnoreCase) <= 0
                ? $"{a}|{b}"
                : $"{b}|{a}";

            if (!seen.Add(duplicateKey))
            {
                errors.Add($"Duplicate connection between {a} and {b}.");
                continue;
            }

            wires.Add(new Wire(a, b));
        }
    }

    private static void ValidatePinReference(
        IReadOnlyDictionary<string, DiagramPart> partsById,
        string partId,
        string pin,
        ICollection<string> errors)
    {
        if (!partsById.TryGetValue(partId, out var part))
        {
            errors.Add($"Connection references invalid part: {partId}.");
            return;
        }

        if (SupportedPins.TryGetValue(part.Type, out var pins) && !pins.Contains(pin))
        {
            errors.Add($"{partId}: pin '{pin}' does not exist on {part.Type}.");
        }
    }

    private static void ValidateComponentWiring(
        IReadOnlyCollection<DiagramPart> parts,
        IReadOnlyDictionary<string, string> connectedPinToNet,
        ICollection<string> errors,
        ICollection<string> warnings)
    {
        var partsById = parts
            .GroupBy(part => part.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var part in parts)
        {
            if (part.Type.Equals("wokwi-led", StringComparison.OrdinalIgnoreCase))
            {
                Require(part, "LED anode must reach an ESP32 GPIO.", HasReachable(part.Id, new[] { "A" }, connectedPinToNet, partsById, IsBoardGpio), errors);
                Require(part, "LED cathode must reach GND.", HasReachable(part.Id, new[] { "C" }, connectedPinToNet, partsById, IsGround), errors);
            }
            else if (part.Type.Equals("wokwi-pushbutton", StringComparison.OrdinalIgnoreCase))
            {
                var buttonPins = new[] { "1.l", "2.l", "1.r", "2.r" };
                Require(part, "Button must connect to an ESP32 GPIO.", HasReachable(part.Id, buttonPins, connectedPinToNet, partsById, IsBoardGpio), errors);
                Require(part, "Button must connect to 3V3 or GND.", HasReachable(part.Id, buttonPins, connectedPinToNet, partsById, IsPowerOrGround), errors);
            }
            else if (part.Type.Equals("wokwi-buzzer", StringComparison.OrdinalIgnoreCase))
            {
                Require(part, "Buzzer must connect to an ESP32 GPIO.", HasReachable(part.Id, new[] { "1", "2" }, connectedPinToNet, partsById, IsBoardGpio), errors);
                Require(part, "Buzzer must connect to GND.", HasReachable(part.Id, new[] { "1", "2" }, connectedPinToNet, partsById, IsGround), errors);
            }
            else if (part.Type.Equals("wokwi-servo", StringComparison.OrdinalIgnoreCase))
            {
                Require(part, "Servo PWM must reach an ESP32 GPIO.", HasReachable(part.Id, new[] { "PWM" }, connectedPinToNet, partsById, IsBoardGpio), errors);
                Require(part, "Servo must connect to GND.", HasReachable(part.Id, new[] { "GND" }, connectedPinToNet, partsById, IsGround), errors);
                Require(part, "Servo power must connect to 3V3/5V.", HasReachable(part.Id, new[] { "V+" }, connectedPinToNet, partsById, IsPower), errors);
            }
            else if (part.Type.Equals("wokwi-dht22", StringComparison.OrdinalIgnoreCase) ||
                     part.Type.Equals("wokwi-dht11", StringComparison.OrdinalIgnoreCase))
            {
                Require(part, "DHT data pin must reach an ESP32 GPIO.", HasReachable(part.Id, new[] { "SDA" }, connectedPinToNet, partsById, IsBoardGpio), errors);
                Require(part, "DHT VCC must connect to 3V3/5V.", HasReachable(part.Id, new[] { "VCC" }, connectedPinToNet, partsById, IsPower), errors);
                Require(part, "DHT GND must connect to GND.", HasReachable(part.Id, new[] { "GND" }, connectedPinToNet, partsById, IsGround), errors);
            }
            else if (part.Type.Equals("wokwi-hc-sr04", StringComparison.OrdinalIgnoreCase))
            {
                Require(part, "Ultrasonic TRIG must reach an ESP32 GPIO.", HasReachable(part.Id, new[] { "TRIG" }, connectedPinToNet, partsById, IsBoardGpio), errors);
                Require(part, "Ultrasonic ECHO must reach an ESP32 GPIO.", HasReachable(part.Id, new[] { "ECHO" }, connectedPinToNet, partsById, IsBoardGpio), errors);
                Require(part, "Ultrasonic VCC must connect to 3V3/5V.", HasReachable(part.Id, new[] { "VCC" }, connectedPinToNet, partsById, IsPower), errors);
                Require(part, "Ultrasonic GND must connect to GND.", HasReachable(part.Id, new[] { "GND" }, connectedPinToNet, partsById, IsGround), errors);
            }
            else if (!Esp32Types.Contains(part.Type) &&
                     !part.Type.Equals("wokwi-resistor", StringComparison.OrdinalIgnoreCase) &&
                     SupportedPins.ContainsKey(part.Type))
            {
                warnings.Add($"{part.Id}: wiring validation for {part.Type} is structural only in MVP.");
            }
        }
    }

    private static bool HasReachable(
        string partId,
        IReadOnlyCollection<string> pins,
        IReadOnlyDictionary<string, string> pinToNet,
        IReadOnlyDictionary<string, DiagramPart> partsById,
        Func<string, string, DiagramPart, bool> predicate)
    {
        foreach (var pin in pins)
        {
            var token = ToPinToken(partId, pin);
            if (!pinToNet.TryGetValue(token, out var netId))
            {
                continue;
            }

            foreach (var candidate in pinToNet.Where(item => item.Value == netId).Select(item => item.Key))
            {
                if (!TryParsePinToken(candidate, out var candidatePartId, out var candidatePin) ||
                    !partsById.TryGetValue(candidatePartId, out var candidatePart))
                {
                    continue;
                }

                if (predicate(candidatePartId, candidatePin, candidatePart))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static NetlistResponse BuildValidationConnectivity(
        IReadOnlyCollection<DiagramPart> parts,
        IReadOnlyCollection<Wire> wires)
    {
        var graphEdges = wires.ToList();
        foreach (var resistor in parts.Where(part => part.Type.Equals("wokwi-resistor", StringComparison.OrdinalIgnoreCase)))
        {
            graphEdges.Add(new Wire(ToPinToken(resistor.Id, "1"), ToPinToken(resistor.Id, "2")));
        }

        return BuildNetlist(graphEdges);
    }

    private static IReadOnlyCollection<VirtualLabRuntimeComponent> BuildRuntimeComponents(
        IReadOnlyCollection<DiagramPart> parts,
        NetlistResponse netlist)
    {
        var components = new List<VirtualLabRuntimeComponent>();
        var partsById = parts
            .GroupBy(part => part.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var part in parts)
        {
            if (Esp32Types.Contains(part.Type) ||
                part.Type.Equals("wokwi-resistor", StringComparison.OrdinalIgnoreCase) ||
                part.Type.Equals("wokwi-gnd", StringComparison.OrdinalIgnoreCase) ||
                part.Type.Equals("wokwi-5v", StringComparison.OrdinalIgnoreCase) ||
                !SupportedPins.TryGetValue(part.Type, out var pins))
            {
                continue;
            }

            var pinToGpio = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pin in pins)
            {
                var token = ToPinToken(part.Id, pin);
                if (!netlist.PinToNet.TryGetValue(token, out var netId))
                {
                    continue;
                }

                var gpio = FindBoardGpioOnNet(netId, netlist, partsById);
                if (gpio != null)
                {
                    pinToGpio[pin] = gpio;
                }
            }

            if (pinToGpio.Count > 0)
            {
                components.Add(new VirtualLabRuntimeComponent(part.Id, part.Type, pinToGpio));
            }
        }

        return components;
    }

    private static string? FindBoardGpioOnNet(
        string netId,
        NetlistResponse netlist,
        IReadOnlyDictionary<string, DiagramPart> partsById)
    {
        foreach (var candidate in netlist.PinToNet.Where(item => item.Value == netId).Select(item => item.Key))
        {
            if (!TryParsePinToken(candidate, out var candidatePartId, out var candidatePin) ||
                !partsById.TryGetValue(candidatePartId, out var candidatePart))
            {
                continue;
            }

            if (IsBoardGpio(candidatePartId, candidatePin, candidatePart))
            {
                return NormalizeGpio(candidatePin);
            }
        }

        return null;
    }

    private static NetlistResponse BuildNetlist(IReadOnlyCollection<Wire> wires)
    {
        if (wires.Count == 0)
        {
            return new NetlistResponse();
        }

        var parent = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        string Find(string pin)
        {
            if (!parent.TryGetValue(pin, out var current))
            {
                parent[pin] = pin;
                return pin;
            }

            if (current.Equals(pin, StringComparison.OrdinalIgnoreCase))
            {
                return current;
            }

            parent[pin] = Find(current);
            return parent[pin];
        }

        void Union(string a, string b)
        {
            var rootA = Find(a);
            var rootB = Find(b);
            if (!rootA.Equals(rootB, StringComparison.OrdinalIgnoreCase))
            {
                parent[rootB] = rootA;
            }
        }

        foreach (var wire in wires)
        {
            Union(wire.A, wire.B);
        }

        var groupedPins = parent.Keys
            .GroupBy(Find, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderBy(pin => pin, StringComparer.OrdinalIgnoreCase).ToArray())
            .OrderBy(group => group[0], StringComparer.OrdinalIgnoreCase)
            .ToList();

        var nets = new List<NetResponse>();
        var pinToNet = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < groupedPins.Count; index++)
        {
            var netId = $"net{index + 1}";
            nets.Add(new NetResponse { Id = netId, Pins = groupedPins[index] });
            foreach (var pin in groupedPins[index])
            {
                pinToNet[pin] = netId;
            }
        }

        return new NetlistResponse
        {
            Nets = nets,
            PinToNet = pinToNet
        };
    }

    private static void Require(DiagramPart part, string message, bool passed, ICollection<string> errors)
    {
        if (!passed)
        {
            errors.Add($"{part.Id}: {message}");
        }
    }

    private static bool IsBoardGpio(string partId, string pin, DiagramPart part)
    {
        return Esp32Types.Contains(part.Type) && IsGpioPin(pin);
    }

    private static bool IsGround(string partId, string pin, DiagramPart part)
    {
        return pin.Equals("GND", StringComparison.OrdinalIgnoreCase) ||
               pin.StartsWith("GND.", StringComparison.OrdinalIgnoreCase) ||
               part.Type.Equals("wokwi-gnd", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPower(string partId, string pin, DiagramPart part)
    {
        return pin.Equals("3V3", StringComparison.OrdinalIgnoreCase) ||
               pin.Equals("3.3V", StringComparison.OrdinalIgnoreCase) ||
               pin.Equals("5V", StringComparison.OrdinalIgnoreCase) ||
               pin.Equals("VCC", StringComparison.OrdinalIgnoreCase) ||
               pin.Equals("VDD", StringComparison.OrdinalIgnoreCase) ||
               pin.Equals("V+", StringComparison.OrdinalIgnoreCase) ||
               part.Type.Equals("wokwi-5v", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPowerOrGround(string partId, string pin, DiagramPart part)
    {
        return IsPower(partId, pin, part) || IsGround(partId, pin, part);
    }

    private static bool IsGpioPin(string pin)
    {
        if (pin.StartsWith("GPIO", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return int.TryParse(pin, out _);
    }

    private static string NormalizeGpio(string pin)
    {
        var trimmed = pin.Trim();
        return trimmed.StartsWith("GPIO", StringComparison.OrdinalIgnoreCase)
            ? trimmed[4..]
            : trimmed;
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;
    }

    private static bool TryParsePinToken(string? token, out string partId, out string pin)
    {
        partId = string.Empty;
        pin = string.Empty;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var separator = token.IndexOf(':');
        if (separator <= 0 || separator == token.Length - 1)
        {
            return false;
        }

        partId = token[..separator].Trim();
        pin = token[(separator + 1)..].Trim();
        return !string.IsNullOrWhiteSpace(partId) && !string.IsNullOrWhiteSpace(pin);
    }

    private static string ToPinToken(string partId, string pin)
    {
        return $"{partId}:{pin}";
    }

    private static HashSet<string> PinSet(params string[] pins)
    {
        return new HashSet<string>(pins, StringComparer.OrdinalIgnoreCase);
    }

    private sealed record DiagramPart(string Id, string Type);

    private sealed record Wire(string A, string B);
}

public sealed record VirtualLabDiagramAnalysis(
    string DiagramJson,
    DiagramValidationResponse Validation,
    NetlistResponse Netlist);

public sealed record VirtualLabRuntimeDiagramSnapshot(
    IReadOnlyCollection<VirtualLabRuntimeComponent> Components);

public sealed record VirtualLabRuntimeComponent(
    string Id,
    string Type,
    IReadOnlyDictionary<string, string> PinToGpio);
