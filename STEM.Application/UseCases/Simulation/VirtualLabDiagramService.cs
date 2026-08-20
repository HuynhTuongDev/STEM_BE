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
            // Pin names match FE POTENTIOMETER_PINS exactly (pinMaps.ts) — SIG is
            // the analog wiper output, GND/VCC are power (same 3-pin convention as
            // wokwi-servo above).
            ["wokwi-potentiometer"] = PinSet("GND", "SIG", "VCC"),
            ["wokwi-dht22"] = PinSet("VCC", "SDA", "NC", "GND"),
            ["wokwi-dht11"] = PinSet("VCC", "SDA", "NC", "GND"),
            ["wokwi-hc-sr04"] = PinSet("VCC", "TRIG", "ECHO", "GND"),
            ["wokwi-lcd1602"] = PinSet("VSS", "VDD", "V0", "RS", "RW", "E", "D0", "D1", "D2", "D3", "D4", "D5", "D6", "D7", "A", "K"),
            // Dọn orphan (2026-07-28): entry cũ PinSet("VCC","GND","SDA","SCL","A","K")
            // sai — A/K là chân backlight chỉ tồn tại ở pins="full" (16 chân
            // song song), không có trong pins="i2c" (4 chân) mà FE thực tế
            // dùng cho LCD 20x4 (xem lcd1602-element.js get pinInfo(), case
            // 'i2c'). Sửa lại đúng 4 chân thật, đồng bộ với FE LCD2004_PINS.
            ["wokwi-lcd2004"] = PinSet("GND", "VCC", "SDA", "SCL"),
            ["wokwi-gnd"] = PinSet("GND"),
            ["wokwi-5v"] = PinSet("5V"),

            // Robot giao hàng mini — chỉ các linh kiện CÓ ĐIỆN mới vào đây.
            // Linh kiện cơ khí/hiển thị thuần (wokwi-robot-wheel, wokwi-caster-wheel,
            // wokwi-robot-chassis, wokwi-breadboard, wokwi-delivery-box) CỐ TÌNH
            // không có entry — ParseParts() hạ chúng xuống warning ("not modeled by
            // the MVP validator") thay vì error, và BuildRuntimeComponents() tự bỏ
            // qua (dòng !SupportedPins.TryGetValue(...) => continue) — im lặng,
            // không tham gia netlist/wiring, không làm Analyze() fail.
            ["wokwi-l298n"] = PinSet(
                "IN1", "IN2", "IN3", "IN4", "ENA", "ENB",
                "OUT1", "OUT2", "OUT3", "OUT4", "VIN", "GND", "5V"),
            ["wokwi-dc-motor"] = PinSet("terminal1", "terminal2"),
            ["wokwi-battery-pack"] = PinSet("+", "-"),
            ["wokwi-power-switch"] = PinSet("IN", "OUT"),

            // Không thuộc Robot giao hàng mini — thêm để chứng minh nhóm
            // "output-easy" (chỉ cần digitalWrite, giống LED/Buzzer/L298N,
            // không cần khả năng mới) có thể mở rộng an toàn theo cùng
            // pattern. Element @wokwi/elements thật (wokwi-rgb-led).
            ["wokwi-rgb-led"] = PinSet("R", "G", "B", "COM"),

            // ===== Thư viện linh kiện mở rộng (Component Library, 2026-07-27)
            // ===== Chỉ các item wiring-validation mới vào đây — dùng chung
            // path "structural only" mặc định trong ValidateComponentWiring()
            // (không có branch riêng, giống wokwi-power-switch). Item
            // visual-only (stepper-motor, ili9341, solenoid-valve, esp32-cam,
            // wifi-cloud-node, dashboard-cloud, và toàn bộ nhóm robot/cơ khí
            // trang trí) CỐ TÌNH KHÔNG có entry — giữ đúng nguyên tắc
            // "visual-only không vào netlist", giống Robot Wheel/Chassis/...
            ["wokwi-flame-sensor"] = PinSet("VCC", "GND", "DOUT", "AOUT"),
            ["wokwi-gas-sensor"] = PinSet("AOUT", "DOUT", "GND", "VCC"),
            ["wokwi-pir-motion-sensor"] = PinSet("VCC", "OUT", "GND"),
            ["wokwi-photoresistor-sensor"] = PinSet("VCC", "GND", "DO", "AO"),
            ["wokwi-ntc-temperature-sensor"] = PinSet("GND", "VCC", "OUT"),
            ["wokwi-hx711"] = PinSet("VCC", "DT", "SCK", "GND"),
            ["wokwi-ir-receiver"] = PinSet("GND", "VCC", "DAT"),
            ["wokwi-membrane-keypad"] = PinSet("R1", "R2", "R3", "R4", "C1", "C2", "C3", "C4"),
            ["wokwi-ssd1306"] = PinSet("DATA", "CLK", "DC", "RST", "CS", "3V3", "VIN", "GND"),
            ["wokwi-lcd1602-i2c"] = PinSet("GND", "VCC", "SDA", "SCL"),
            ["wokwi-neopixel"] = PinSet("VDD", "DOUT", "VSS", "DIN"),
            ["wokwi-led-bar-graph"] = PinSet(
                "A1", "A2", "A3", "A4", "A5", "A6", "A7", "A8", "A9", "A10",
                "C1", "C2", "C3", "C4", "C5", "C6", "C7", "C8", "C9", "C10"),
            ["wokwi-7segment"] = PinSet("COM.1", "COM.2", "A", "B", "C", "D", "E", "F", "G", "DP"),
            ["wokwi-relay-module"] = PinSet("VCC", "IN", "GND", "NO", "COM", "NC"),
            ["wokwi-fan"] = PinSet("+", "-"),
            ["wokwi-water-pump"] = PinSet("+", "-"),
            ["wokwi-water-leak-sensor"] = PinSet("VCC", "GND", "S"),
            ["wokwi-rain-sensor"] = PinSet("VCC", "GND", "DO", "AO"),
            ["wokwi-soil-moisture-sensor"] = PinSet("VCC", "GND", "DO", "AO"),
            ["wokwi-ir-obstacle-sensor"] = PinSet("VCC", "GND", "OUT"),
            ["wokwi-line-tracking-sensor"] = PinSet("VCC", "GND", "OUT"),
            ["wokwi-color-sensor"] = PinSet("VCC", "GND", "SDA", "SCL"),
            ["wokwi-vibration-sensor"] = PinSet("VCC", "GND", "OUT"),

            // ===== Component mới (2026-07-28, task "pin/visual chuẩn theo thực
            // tế") ===== wokwi-mpu6050: element thật @wokwi/elements, 8 chân lấy
            // trực tiếp từ pinInfo. wokwi-esc/wokwi-heating-element/wokwi-ph-sensor:
            // không có trong Wokwi, tự định nghĩa theo module thực tế phổ biến —
            // xem robotKitComponents.ts (FE) để biết nguồn tham khảo.
            ["wokwi-mpu6050"] = PinSet("VCC", "GND", "SCL", "SDA", "XDA", "XCL", "AD0", "INT"),
            ["wokwi-esc"] = PinSet("SIG", "GND", "BATT+", "BATT-", "OUT+", "OUT-"),
            ["wokwi-heating-element"] = PinSet("+", "-"),
            ["wokwi-ph-sensor"] = PinSet("VCC", "GND", "PO"),

            // Line Tracking đa kênh (module TCRT5000 3/5 mắt) — BỔ SUNG bên
            // cạnh wokwi-line-tracking-sensor (1 kênh) cũ, không thay thế.
            ["wokwi-line-tracking-3ch"] = PinSet("VCC", "GND", "OUT1", "OUT2", "OUT3"),
            ["wokwi-line-tracking-5ch"] = PinSet("VCC", "GND", "OUT1", "OUT2", "OUT3", "OUT4", "OUT5")
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
            else if (part.Type.Equals("wokwi-potentiometer", StringComparison.OrdinalIgnoreCase))
            {
                Require(part, "Potentiometer SIG must reach an ESP32 GPIO.", HasReachable(part.Id, new[] { "SIG" }, connectedPinToNet, partsById, IsBoardGpio), errors);
                Require(part, "Potentiometer must connect to GND.", HasReachable(part.Id, new[] { "GND" }, connectedPinToNet, partsById, IsGround), errors);
                Require(part, "Potentiometer power must connect to 3V3/5V.", HasReachable(part.Id, new[] { "VCC" }, connectedPinToNet, partsById, IsPower), errors);
            }
            else if (part.Type.Equals("wokwi-photoresistor-sensor", StringComparison.OrdinalIgnoreCase))
            {
                // AO (analog out) is what LightSensorModel actually reads via
                // analogRead() — same shape as Potentiometer's SIG. DO (digital
                // out / threshold comparator pin) exists on the real module but
                // isn't runtime-supported here, same "wiring-validation only for
                // pins we don't model" pattern already used elsewhere in this file.
                Require(part, "Photoresistor AO must reach an ESP32 GPIO.", HasReachable(part.Id, new[] { "AO" }, connectedPinToNet, partsById, IsBoardGpio), errors);
                Require(part, "Photoresistor must connect to GND.", HasReachable(part.Id, new[] { "GND" }, connectedPinToNet, partsById, IsGround), errors);
                Require(part, "Photoresistor power must connect to 3V3/5V.", HasReachable(part.Id, new[] { "VCC" }, connectedPinToNet, partsById, IsPower), errors);
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
            else if (part.Type.Equals("wokwi-l298n", StringComparison.OrdinalIgnoreCase))
            {
                Require(part, "L298N IN1 must reach an ESP32 GPIO.", HasReachable(part.Id, new[] { "IN1" }, connectedPinToNet, partsById, IsBoardGpio), errors);
                Require(part, "L298N IN2 must reach an ESP32 GPIO.", HasReachable(part.Id, new[] { "IN2" }, connectedPinToNet, partsById, IsBoardGpio), errors);
                Require(part, "L298N IN3 must reach an ESP32 GPIO.", HasReachable(part.Id, new[] { "IN3" }, connectedPinToNet, partsById, IsBoardGpio), errors);
                Require(part, "L298N IN4 must reach an ESP32 GPIO.", HasReachable(part.Id, new[] { "IN4" }, connectedPinToNet, partsById, IsBoardGpio), errors);
                Require(part, "L298N VIN/VCC must connect to Battery Pack (+) or another power source.", HasReachable(part.Id, new[] { "VIN" }, connectedPinToNet, partsById, IsPowerOrBatteryPositive), errors);
                Require(part, "L298N GND must connect to common ground.", HasReachable(part.Id, new[] { "GND" }, connectedPinToNet, partsById, IsGroundOrBatteryNegative), errors);

                // ENA/ENB thường được nối tắt bằng jumper có sẵn trên module thật
                // (full-speed mặc định) thay vì nối dây ra GPIO/PWM — không đủ căn
                // cứ để coi là lỗi, chỉ nhắc để giáo viên/học sinh biết mà kiểm tra.
                if (!connectedPinToNet.ContainsKey(ToPinToken(part.Id, "ENA")))
                {
                    warnings.Add($"{part.Id}: ENA chưa được nối — nếu module dùng jumper mặc định thì bỏ qua, nếu không hãy nối ENA tới GPIO/PWM.");
                }

                if (!connectedPinToNet.ContainsKey(ToPinToken(part.Id, "ENB")))
                {
                    warnings.Add($"{part.Id}: ENB chưa được nối — nếu module dùng jumper mặc định thì bỏ qua, nếu không hãy nối ENB tới GPIO/PWM.");
                }
            }
            else if (part.Type.Equals("wokwi-dc-motor", StringComparison.OrdinalIgnoreCase))
            {
                // Động cơ DC hút dòng vượt xa khả năng chịu tải của 1 chân GPIO
                // ESP32 — bắt buộc phải đi qua OUT của L298N, không được cấp trực
                // tiếp từ GPIO. HasReachable trả true nghĩa là ĐANG nối sai (nối
                // thẳng vào GPIO), nên passed = phủ định của nó.
                var wiredDirectlyToGpio = HasReachable(part.Id, new[] { "terminal1", "terminal2" }, connectedPinToNet, partsById, IsBoardGpio);
                Require(part, "Không được nối động cơ DC trực tiếp vào GPIO ESP32 — phải qua OUT của L298N Motor Driver.", !wiredDirectlyToGpio, errors);
            }
            else if (part.Type.Equals("wokwi-rgb-led", StringComparison.OrdinalIgnoreCase))
            {
                Require(part, "RGB LED chân R phải reach 1 GPIO ESP32.", HasReachable(part.Id, new[] { "R" }, connectedPinToNet, partsById, IsBoardGpio), errors);
                Require(part, "RGB LED chân G phải reach 1 GPIO ESP32.", HasReachable(part.Id, new[] { "G" }, connectedPinToNet, partsById, IsBoardGpio), errors);
                Require(part, "RGB LED chân B phải reach 1 GPIO ESP32.", HasReachable(part.Id, new[] { "B" }, connectedPinToNet, partsById, IsBoardGpio), errors);
                Require(part, "RGB LED chân COM phải reach GND hoặc nguồn (tuỳ common-anode/cathode).", HasReachable(part.Id, new[] { "COM" }, connectedPinToNet, partsById, IsPowerOrGround), errors);
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

    // Battery Pack không có nhãn pin chuẩn (không phải "VCC"/"GND" như các
    // linh kiện khác) — cực "+"/"-" của nó cần được công nhận là nguồn điện/
    // ground hợp lệ khi L298N tham chiếu tới, dù pin name không khớp IsPower/
    // IsGround thông thường. Không mô phỏng điện áp 7.4V thật (theo đúng
    // phạm vi phase này) — chỉ công nhận đúng cực tính cho mục đích wiring.
    private static bool IsBatteryPositiveTerminal(string partId, string pin, DiagramPart part)
    {
        return part.Type.Equals("wokwi-battery-pack", StringComparison.OrdinalIgnoreCase) &&
               pin.Equals("+", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPowerOrBatteryPositive(string partId, string pin, DiagramPart part)
    {
        return IsPower(partId, pin, part) || IsBatteryPositiveTerminal(partId, pin, part);
    }

    private static bool IsGroundOrBatteryNegative(string partId, string pin, DiagramPart part)
    {
        return IsGround(partId, pin, part) ||
               (part.Type.Equals("wokwi-battery-pack", StringComparison.OrdinalIgnoreCase) &&
                pin.Equals("-", StringComparison.OrdinalIgnoreCase));
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
