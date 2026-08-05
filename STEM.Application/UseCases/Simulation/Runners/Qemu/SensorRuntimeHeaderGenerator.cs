using System.Globalization;
using System.Text;
using System.Text.Json;
using STEM.Application.Dtos.Simulation;

namespace STEM.Application.UseCases.Simulation.Runners.Qemu;

// Sensor Input Bridge — Phase 1+2 (scenario/timeline). Sinh 1 header C++ tự
// chứa (không cần channel giao tiếp 2 chiều mới với QEMU — điều tra thật
// trước đó (VIRTUAL_LAB_PLAN.md mục "HC-SR04 — Điều tra thật khả năng inject
// GPIO input") đã xác nhận QEMU KHÔNG có QOM property/QMP command nào cho
// host set GPIO input lúc máy đang chạy; approach ở đây né hẳn vấn đề đó
// bằng cách nhúng TOÀN BỘ timeline vào firmware NGAY LÚC COMPILE — firmware
// tự tra bảng theo millis() nó tự đọc được, không cần host can thiệp gì lúc
// runtime).
//
// An toàn với sourceCode học sinh — theo ĐÚNG pattern GpioInstrumentationPreamble
// (FirmwareCacheService.cs) đã verify: định nghĩa hàm wrapper TRƯỚC (lúc đó lời
// gọi digitalRead/analogRead/pulseIn THẬT bên trong wrapper vẫn còn trỏ tới hàm
// gốc, chưa bị macro thay), rồi mới #define SAU — tránh đệ quy vô hạn (bug đã
// từng gặp thật với digitalWrite, xem comment gốc).
//
// BUG THẬT đã vá ở Phase 1 (2026-07-28, xác nhận qua compile thật trong Docker
// sandbox, KHÔNG suy đoán) — áp dụng CHO MỌI hàm top-level sinh ra ở đây:
// 1) arduino-cli tự động sinh forward-declaration (prototype) cho MỌI hàm
//    top-level trong .ino và chèn chúng lên ĐẦU file — struct tự định nghĩa
//    dùng làm tham số hàm sẽ gây lỗi "does not name a type" dù struct đã
//    khai báo trước trong cùng file (prototype tham chiếu tới struct TRƯỚC
//    KHI struct được định nghĩa ở vị trí thật). => KHÔNG dùng struct trong
//    chữ ký hàm top-level nào — chỉ dùng mảng song song kiểu nguyên thuỷ.
// 2) Cùng lý do, default argument (`= giá trị`) trên hàm top-level bị nhân
//    đôi giữa auto-prototype và định nghĩa thật => lỗi "default argument
//    specified in both declaration and definition". => dùng overload tường
//    minh thay vì default argument khi cần hỗ trợ nhiều số lượng tham số
//    (pulseIn 2 hoặc 3 tham số).
public static class SensorRuntimeHeaderGenerator
{
    private const string HcSr04Type = "wokwi-hc-sr04";
    private const string PirType = "wokwi-pir-motion-sensor";
    private const string LineTracking3ChType = "wokwi-line-tracking-3ch";
    private const string LineTracking5ChType = "wokwi-line-tracking-5ch";
    private const string WaterLeakType = "wokwi-water-leak-sensor";
    private const string FlameType = "wokwi-flame-sensor";
    private const string SoilMoistureType = "wokwi-soil-moisture-sensor";
    private const string RainType = "wokwi-rain-sensor";
    private const string VibrationType = "wokwi-vibration-sensor";
    private const string Dht22Type = "wokwi-dht22";
    private const string Dht11Type = "wokwi-dht11";

    private const double DefaultDistanceCm = 400.0; // "vật ở xa, an toàn" — giá trị mặc định trung tính
    private const bool DefaultMotion = false;
    private const int DefaultAnalogLow = 300;   // detected=false — giống ví dụ user cho Water Leak
    private const int DefaultAnalogHigh = 2800; // detected=true
    private const double DefaultTemperatureC = 25.0;
    private const double DefaultHumidityPct = 50.0;

    // Phase 2.1 — Line Tracking pattern -> giá trị từng kênh OUT (true=HIGH
    // phát hiện line tại mắt đó). Pattern KHÔNG nằm trong bảng -> coi như
    // "lost" (an toàn, không suy đoán giá trị lạ). 5 kênh là suy diễn hợp lý
    // từ mô tả "tương tự" của user (mỗi mắt bật đúng 1 vị trí, lost=tất cả
    // LOW, intersection=tất cả HIGH) — GHI RÕ đây là lựa chọn thiết kế, không
    // phải số liệu datasheet cụ thể.
    private static readonly Dictionary<string, bool[]> LineTracking3ChPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["center"] = new[] { false, true, false },
        ["left"] = new[] { true, false, false },
        ["right"] = new[] { false, false, true },
        ["lost"] = new[] { false, false, false },
        ["intersection"] = new[] { true, true, true },
    };

    private static readonly Dictionary<string, bool[]> LineTracking5ChPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["far-left"] = new[] { true, false, false, false, false },
        ["left"] = new[] { false, true, false, false, false },
        ["center"] = new[] { false, false, true, false, false },
        ["right"] = new[] { false, false, false, true, false },
        ["far-right"] = new[] { false, false, false, false, true },
        ["lost"] = new[] { false, false, false, false, false },
        ["intersection"] = new[] { true, true, true, true, true },
    };

    // Phase 2.2 — cấu hình pin theo từng loại sensor digital/analog chung.
    // DigitalPin=null nghĩa là sensor đó không có digitalRead (không xảy ra
    // trong nhóm này); AnalogPin=null nghĩa là không có chân analog riêng
    // (Vibration Sensor SW-420 thật không có AOUT).
    private sealed record GenericSensorPinConfig(string DigitalPin, string? AnalogPin);

    private static readonly Dictionary<string, GenericSensorPinConfig> GenericSensorPins = new(StringComparer.OrdinalIgnoreCase)
    {
        [WaterLeakType] = new GenericSensorPinConfig("S", "S"),
        [FlameType] = new GenericSensorPinConfig("DOUT", "AOUT"),
        [SoilMoistureType] = new GenericSensorPinConfig("DO", "AO"),
        [RainType] = new GenericSensorPinConfig("DO", "AO"),
        [VibrationType] = new GenericSensorPinConfig("OUT", null),
    };

    public static SensorScenarioConfig? TryParseScenario(string diagramJson)
    {
        if (string.IsNullOrWhiteSpace(diagramJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(diagramJson);
            if (!document.RootElement.TryGetProperty("sensorScenario", out var scenarioEl) ||
                scenarioEl.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            return JsonSerializer.Deserialize<SensorScenarioConfig>(
                scenarioEl.GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            // diagramJson lỗi cú pháp đã bị VirtualLabDiagramService.Analyze() bắt
            // và báo lỗi riêng — ở đây chỉ coi như "không có scenario" thay vì
            // crash lần đọc thứ 2 này.
            return null;
        }
    }

    // Trả null nếu không có gì để inject (không sensor nào được wire + hỗ trợ)
    // — QemuEsp32Runner PHẢI coi null là "không đổi gì cả" (giữ nguyên
    // sensorHeader="" mặc định, cache key không đổi).
    public static string? Generate(VirtualLabRuntimeDiagramSnapshot snapshot, SensorScenarioConfig? scenario)
    {
        var infoBlocks = new List<string>();
        var digitalReadCases = new List<string>();
        var analogReadCases = new List<string>();
        var pulseInCases = new List<string>();
        var declarations = new StringBuilder();
        var dhtBranches = new List<(string TempCheck, string HumCheck)>();
        var dhtDeclarations = new StringBuilder();

        var instanceCounter = 0;

        foreach (var component in snapshot.Components)
        {
            var scenarioTimeline = scenario?.Sensors.TryGetValue(component.Id, out var t) == true ? t.Timeline : null;

            if (component.Type.Equals(HcSr04Type, StringComparison.OrdinalIgnoreCase))
            {
                if (!component.PinToGpio.TryGetValue("TRIG", out var trigPin) ||
                    !component.PinToGpio.TryGetValue("ECHO", out var echoPin) ||
                    string.IsNullOrEmpty(trigPin) || string.IsNullOrEmpty(echoPin))
                {
                    continue; // chưa nối đủ TRIG+ECHO — không có gì để giả lập
                }

                var arrayName = $"__sf_hcsr04_{instanceCounter++}_{SanitizeId(component.Id)}";
                AppendDistanceArray(declarations, arrayName, scenarioTimeline);
                pulseInCases.Add(
                    $$"""
                          case {{echoPin}}: {
                            float __sf_d = __sf_lookupFloat({{arrayName}}_t, {{arrayName}}_v, {{arrayName}}_len, millis(), {{FormatDouble(DefaultDistanceCm)}});
                            return (unsigned long)(__sf_d * 58.0f);
                          }
                    """);
                infoBlocks.Add($"// HC-SR04 component={component.Id} TRIG={trigPin} ECHO={echoPin}");
            }
            else if (component.Type.Equals(PirType, StringComparison.OrdinalIgnoreCase))
            {
                if (!component.PinToGpio.TryGetValue("OUT", out var outPin) || string.IsNullOrEmpty(outPin))
                {
                    continue;
                }

                var arrayName = $"__sf_pir_{instanceCounter++}_{SanitizeId(component.Id)}";
                AppendMotionArray(declarations, arrayName, scenarioTimeline, e => e.Motion, DefaultMotion);
                digitalReadCases.Add(
                    $$"""
                          case {{outPin}}:
                            return __sf_lookupBool({{arrayName}}_t, {{arrayName}}_v, {{arrayName}}_len, millis(), {{(DefaultMotion ? "true" : "false")}}) ? HIGH : LOW;
                    """);
                infoBlocks.Add($"// PIR component={component.Id} OUT={outPin}");
            }
            else if (component.Type.Equals(LineTracking3ChType, StringComparison.OrdinalIgnoreCase) ||
                     component.Type.Equals(LineTracking5ChType, StringComparison.OrdinalIgnoreCase))
            {
                var channelCount = component.Type.Equals(LineTracking3ChType, StringComparison.OrdinalIgnoreCase) ? 3 : 5;
                var patternTable = channelCount == 3 ? LineTracking3ChPatterns : LineTracking5ChPatterns;
                var pins = new string[channelCount];
                var allWired = true;
                for (var i = 0; i < channelCount; i++)
                {
                    if (!component.PinToGpio.TryGetValue($"OUT{i + 1}", out var p) || string.IsNullOrEmpty(p))
                    {
                        allWired = false;
                        break;
                    }
                    pins[i] = p;
                }
                if (!allWired) continue;

                var baseName = $"__sf_linetrack_{instanceCounter++}_{SanitizeId(component.Id)}";
                AppendLineTrackingArrays(declarations, baseName, scenarioTimeline, channelCount, patternTable);
                for (var i = 0; i < channelCount; i++)
                {
                    digitalReadCases.Add(
                        $$"""
                              case {{pins[i]}}:
                                return __sf_lookupBool({{baseName}}_t, {{baseName}}_ch{{i}}_v, {{baseName}}_len, millis(), false) ? HIGH : LOW;
                        """);
                }
                infoBlocks.Add($"// Line Tracking {channelCount}ch component={component.Id} OUT1..{channelCount}={string.Join(",", pins)}");
            }
            else if (GenericSensorPins.TryGetValue(component.Type, out var pinConfig))
            {
                if (!component.PinToGpio.TryGetValue(pinConfig.DigitalPin, out var digitalPin) || string.IsNullOrEmpty(digitalPin))
                {
                    continue;
                }

                var arrayName = $"__sf_gen_{instanceCounter++}_{SanitizeId(component.Id)}";
                AppendDetectedArray(declarations, arrayName, scenarioTimeline);
                digitalReadCases.Add(
                    $$"""
                          case {{digitalPin}}:
                            return __sf_lookupBool({{arrayName}}_t, {{arrayName}}_detected_v, {{arrayName}}_len, millis(), false) ? HIGH : LOW;
                    """);
                infoBlocks.Add($"// {component.Type} component={component.Id} digital={digitalPin}");

                if (pinConfig.AnalogPin != null &&
                    component.PinToGpio.TryGetValue(pinConfig.AnalogPin, out var analogPin) &&
                    !string.IsNullOrEmpty(analogPin))
                {
                    // Water Leak: AnalogPin == DigitalPin (cùng 1 chân vật lý,
                    // 2 cách đọc khác nhau tuỳ sketch dùng digitalRead hay
                    // analogRead) — mảng analog TÁI SỬ DỤNG cùng mốc thời
                    // gian (_t) đã sinh ở trên, chỉ thêm mảng giá trị analog.
                    AppendAnalogArray(declarations, arrayName, scenarioTimeline);
                    analogReadCases.Add(
                        $$"""
                              case {{analogPin}}:
                                return (int)__sf_lookupFloat({{arrayName}}_t, {{arrayName}}_analog_v, {{arrayName}}_len, millis(), {{DefaultAnalogLow}}.0f);
                        """);
                    if (!analogPin.Equals(digitalPin, StringComparison.OrdinalIgnoreCase))
                    {
                        infoBlocks.Add($"// {component.Type} component={component.Id} analog={analogPin}");
                    }
                }
            }
            else if (component.Type.Equals(Dht22Type, StringComparison.OrdinalIgnoreCase) ||
                     component.Type.Equals(Dht11Type, StringComparison.OrdinalIgnoreCase))
            {
                // DHT KHÔNG qua digitalRead/pulseIn/analogRead — giao thức 1-wire
                // timing riêng của DHT thật không thể giả lập tin cậy qua các
                // wrapper đó (đọc quá nhanh/timing sai sẽ luôn ra NaN với thư
                // viện DHT thật). Chỉ hỗ trợ qua StemFlowDHT helper riêng — user
                // phải chủ động dùng class này trong sketch, KHÔNG tự động thay
                // thế DHT.h thật.
                var safeId = SanitizeId(component.Id);
                var tempArray = $"__sf_dht_{instanceCounter}_temp";
                var humArray = $"__sf_dht_{instanceCounter}_hum";
                instanceCounter++;
                AppendTemperatureArray(dhtDeclarations, tempArray, scenarioTimeline);
                AppendHumidityArray(dhtDeclarations, humArray, scenarioTimeline);
                dhtBranches.Add((
                    $"    if (strcmp(_id, \"{EscapeCString(component.Id)}\") == 0) return __sf_lookupFloat({tempArray}_t, {tempArray}_v, {tempArray}_len, millis(), {FormatDouble(DefaultTemperatureC)});",
                    $"    if (strcmp(_id, \"{EscapeCString(component.Id)}\") == 0) return __sf_lookupFloat({humArray}_t, {humArray}_v, {humArray}_len, millis(), {FormatDouble(DefaultHumidityPct)});"
                ));
                infoBlocks.Add($"// {component.Type} component={component.Id} (StemFlowDHT helper, id=\"{component.Id}\")");
            }
        }

        if (digitalReadCases.Count == 0 && pulseInCases.Count == 0 && analogReadCases.Count == 0 && dhtBranches.Count == 0)
        {
            return null;
        }

        var sb = new StringBuilder();
        sb.AppendLine("// ---- StemFlow Sensor Input Bridge (Phase 1+2, scenario/timeline, auto-injected) ----");
        sb.AppendLine("// Giá trị KHÔNG phải cảm biến vật lý mô phỏng thật (không ray-casting, không đo");
        sb.AppendLine("// khoảng cách/ánh sáng/độ ẩm theo scene) — là kịch bản do giáo viên/học sinh cấu");
        sb.AppendLine("// hình qua UI, firmware ĐỌC THẬT giá trị này qua digitalRead()/analogRead()/");
        sb.AppendLine("// pulseIn() bị tiêm macro dưới đây (riêng DHT qua StemFlowDHT helper, KHÔNG macro).");
        foreach (var b in infoBlocks) sb.AppendLine(b);
        // Mảng song song (KHÔNG dùng struct — xem comment lớp trên) — an toàn
        // với cơ chế auto-prototype của arduino-cli vì chỉ dùng kiểu nguyên
        // thuỷ trong MỌI chữ ký hàm top-level bên dưới.
        sb.Append(declarations);
        sb.AppendLine();
        sb.AppendLine("static bool __sf_lookupBool(const unsigned long* t, const bool* v, int len, unsigned long now, bool defVal) {");
        sb.AppendLine("  if (len == 0) return defVal;");
        sb.AppendLine("  bool result = v[0];");
        sb.AppendLine("  for (int i = 0; i < len; i++) { if (t[i] <= now) result = v[i]; else break; }");
        sb.AppendLine("  return result;");
        sb.AppendLine("}");
        sb.AppendLine("static float __sf_lookupFloat(const unsigned long* t, const float* v, int len, unsigned long now, float defVal) {");
        sb.AppendLine("  if (len == 0) return defVal;");
        sb.AppendLine("  float result = v[0];");
        sb.AppendLine("  for (int i = 0; i < len; i++) { if (t[i] <= now) result = v[i]; else break; }");
        sb.AppendLine("  return result;");
        sb.AppendLine("}");
        sb.AppendLine();

        if (digitalReadCases.Count > 0)
        {
            sb.AppendLine("int __sf_digitalRead(uint8_t pin) {");
            sb.AppendLine("  switch (pin) {");
            foreach (var c in digitalReadCases) sb.AppendLine(c);
            sb.AppendLine("    default: return digitalRead(pin);");
            sb.AppendLine("  }");
            sb.AppendLine("}");
            sb.AppendLine("#define digitalRead(pin) __sf_digitalRead(pin)");
        }

        if (analogReadCases.Count > 0)
        {
            sb.AppendLine("int __sf_analogRead(uint8_t pin) {");
            sb.AppendLine("  switch (pin) {");
            foreach (var c in analogReadCases) sb.AppendLine(c);
            sb.AppendLine("    default: return analogRead(pin);");
            sb.AppendLine("  }");
            sb.AppendLine("}");
            sb.AppendLine("#define analogRead(pin) __sf_analogRead(pin)");
        }

        if (pulseInCases.Count > 0)
        {
            sb.AppendLine("unsigned long __sf_pulseIn(uint8_t pin, uint8_t state, unsigned long timeout) {");
            sb.AppendLine("  switch (pin) {");
            foreach (var c in pulseInCases) sb.AppendLine(c);
            sb.AppendLine("    default: return pulseIn(pin, state, timeout);");
            sb.AppendLine("  }");
            sb.AppendLine("}");
            // Overload 2 tham số (KHÔNG dùng default argument — xem comment lớp
            // trên, mục 2).
            sb.AppendLine("unsigned long __sf_pulseIn(uint8_t pin, uint8_t state) {");
            sb.AppendLine("  return __sf_pulseIn(pin, state, 1000000UL);");
            sb.AppendLine("}");
            sb.AppendLine("#define pulseIn(...) __sf_pulseIn(__VA_ARGS__)");
        }

        if (dhtBranches.Count > 0)
        {
            sb.AppendLine("#include <string.h>");
            sb.Append(dhtDeclarations);
            sb.AppendLine();
            sb.AppendLine("// StemFlowDHT — helper RIÊNG cho Phase 1+2, KHÔNG phải thư viện DHT.h thật, KHÔNG");
            sb.AppendLine("// đọc giao thức 1-wire timing thật. Chỉ hỗ trợ đúng 2 hàm readTemperature()/");
            sb.AppendLine("// readHumidity() theo scenario — sketch PHẢI include \"StemFlowDHT.h\" và dùng class");
            sb.AppendLine("// này trực tiếp, KHÔNG tự động thay thế DHT.h thật nếu sketch đang dùng thư viện đó.");
            sb.AppendLine("class StemFlowDHT {");
            sb.AppendLine("  public:");
            sb.AppendLine("    StemFlowDHT(const char* id) : _id(id) {}");
            sb.AppendLine("    float readTemperature() {");
            foreach (var (tempCheck, _) in dhtBranches) sb.AppendLine(tempCheck);
            sb.AppendLine($"      return {FormatDouble(DefaultTemperatureC)};");
            sb.AppendLine("    }");
            sb.AppendLine("    float readHumidity() {");
            foreach (var (_, humCheck) in dhtBranches) sb.AppendLine(humCheck);
            sb.AppendLine($"      return {FormatDouble(DefaultHumidityPct)};");
            sb.AppendLine("    }");
            sb.AppendLine("  private:");
            sb.AppendLine("    const char* _id;");
            sb.AppendLine("};");
        }

        sb.AppendLine("// ---- end StemFlow Sensor Input Bridge ----");
        sb.AppendLine();
        return sb.ToString();
    }

    private static void AppendDistanceArray(StringBuilder sb, string arrayName, List<SensorTimelineEntry>? timeline)
    {
        var entries = (timeline ?? new List<SensorTimelineEntry>())
            .Where(e => e.DistanceCm.HasValue)
            .OrderBy(e => e.TimeMs)
            .ToList();
        if (entries.Count == 0)
        {
            entries.Add(new SensorTimelineEntry { TimeMs = 0, DistanceCm = DefaultDistanceCm });
        }

        sb.AppendLine($"static const unsigned long {arrayName}_t[] = {{ {string.Join(", ", entries.Select(e => $"{e.TimeMs}UL"))} }};");
        sb.AppendLine($"static const float {arrayName}_v[] = {{ {string.Join(", ", entries.Select(e => FormatDouble(e.DistanceCm!.Value)))} }};");
        sb.AppendLine($"static const int {arrayName}_len = {entries.Count};");
    }

    private static void AppendMotionArray(
        StringBuilder sb, string arrayName, List<SensorTimelineEntry>? timeline, Func<SensorTimelineEntry, bool?> selector, bool defaultValue)
    {
        var entries = (timeline ?? new List<SensorTimelineEntry>())
            .Where(e => selector(e).HasValue)
            .OrderBy(e => e.TimeMs)
            .ToList();
        if (entries.Count == 0)
        {
            entries.Add(new SensorTimelineEntry { TimeMs = 0, Motion = defaultValue });
        }

        sb.AppendLine($"static const unsigned long {arrayName}_t[] = {{ {string.Join(", ", entries.Select(e => $"{e.TimeMs}UL"))} }};");
        sb.AppendLine($"static const bool {arrayName}_v[] = {{ {string.Join(", ", entries.Select(e => (selector(e) ?? defaultValue) ? "true" : "false"))} }};");
        sb.AppendLine($"static const int {arrayName}_len = {entries.Count};");
    }

    // Phase 2.1 — sinh mảng thời gian dùng chung + N mảng bool (1 mảng/kênh),
    // quy đổi từ Pattern qua bảng tra LineTracking{3,5}ChPatterns. Pattern
    // không có trong timeline (entry khác dùng field khác) hoặc không khớp
    // bảng tra -> coi như "lost".
    private static void AppendLineTrackingArrays(
        StringBuilder sb, string baseName, List<SensorTimelineEntry>? timeline, int channelCount, Dictionary<string, bool[]> patternTable)
    {
        var entries = (timeline ?? new List<SensorTimelineEntry>())
            .Where(e => e.Pattern != null)
            .OrderBy(e => e.TimeMs)
            .ToList();
        if (entries.Count == 0)
        {
            entries.Add(new SensorTimelineEntry { TimeMs = 0, Pattern = "lost" });
        }

        var resolved = entries
            .Select(e => (e.TimeMs, Channels: patternTable.TryGetValue(e.Pattern!, out var ch) ? ch : patternTable["lost"]))
            .ToList();

        sb.AppendLine($"static const unsigned long {baseName}_t[] = {{ {string.Join(", ", resolved.Select(e => $"{e.TimeMs}UL"))} }};");
        for (var ch = 0; ch < channelCount; ch++)
        {
            sb.AppendLine($"static const bool {baseName}_ch{ch}_v[] = {{ {string.Join(", ", resolved.Select(e => e.Channels[ch] ? "true" : "false"))} }};");
        }
        sb.AppendLine($"static const int {baseName}_len = {resolved.Count};");
    }

    private static void AppendDetectedArray(StringBuilder sb, string arrayName, List<SensorTimelineEntry>? timeline)
    {
        var entries = (timeline ?? new List<SensorTimelineEntry>())
            .Where(e => e.Detected.HasValue)
            .OrderBy(e => e.TimeMs)
            .ToList();
        if (entries.Count == 0)
        {
            entries.Add(new SensorTimelineEntry { TimeMs = 0, Detected = false });
        }

        sb.AppendLine($"static const unsigned long {arrayName}_t[] = {{ {string.Join(", ", entries.Select(e => $"{e.TimeMs}UL"))} }};");
        sb.AppendLine($"static const bool {arrayName}_detected_v[] = {{ {string.Join(", ", entries.Select(e => (e.Detected ?? false) ? "true" : "false"))} }};");
        sb.AppendLine($"static const int {arrayName}_len = {entries.Count};");
    }

    // Tái sử dụng {arrayName}_t đã sinh ở AppendDetectedArray (CÙNG số lượng
    // mốc thời gian — do cùng lấy từ đúng 1 danh sách `entries` lọc theo
    // Detected.HasValue; nếu 1 entry có Analog nhưng KHÔNG có Detected, giá
    // trị analog đó sẽ bị bỏ qua — hạn chế đã biết, ghi rõ trong Limitation:
    // mỗi mốc thời gian dùng cho sensor nhóm 2.2 nên luôn set cả Detected).
    private static void AppendAnalogArray(StringBuilder sb, string arrayName, List<SensorTimelineEntry>? timeline)
    {
        var entries = (timeline ?? new List<SensorTimelineEntry>())
            .Where(e => e.Detected.HasValue)
            .OrderBy(e => e.TimeMs)
            .ToList();
        if (entries.Count == 0)
        {
            entries.Add(new SensorTimelineEntry { TimeMs = 0, Detected = false });
        }

        sb.AppendLine($"static const float {arrayName}_analog_v[] = {{ {string.Join(", ", entries.Select(e => FormatDouble(e.Analog ?? ((e.Detected ?? false) ? DefaultAnalogHigh : DefaultAnalogLow))))} }};");
    }

    private static void AppendTemperatureArray(StringBuilder sb, string arrayName, List<SensorTimelineEntry>? timeline)
    {
        var entries = (timeline ?? new List<SensorTimelineEntry>())
            .Where(e => e.Temperature.HasValue)
            .OrderBy(e => e.TimeMs)
            .ToList();
        if (entries.Count == 0)
        {
            entries.Add(new SensorTimelineEntry { TimeMs = 0, Temperature = DefaultTemperatureC });
        }

        sb.AppendLine($"static const unsigned long {arrayName}_t[] = {{ {string.Join(", ", entries.Select(e => $"{e.TimeMs}UL"))} }};");
        sb.AppendLine($"static const float {arrayName}_v[] = {{ {string.Join(", ", entries.Select(e => FormatDouble(e.Temperature!.Value)))} }};");
        sb.AppendLine($"static const int {arrayName}_len = {entries.Count};");
    }

    private static void AppendHumidityArray(StringBuilder sb, string arrayName, List<SensorTimelineEntry>? timeline)
    {
        var entries = (timeline ?? new List<SensorTimelineEntry>())
            .Where(e => e.Humidity.HasValue)
            .OrderBy(e => e.TimeMs)
            .ToList();
        if (entries.Count == 0)
        {
            entries.Add(new SensorTimelineEntry { TimeMs = 0, Humidity = DefaultHumidityPct });
        }

        sb.AppendLine($"static const unsigned long {arrayName}_t[] = {{ {string.Join(", ", entries.Select(e => $"{e.TimeMs}UL"))} }};");
        sb.AppendLine($"static const float {arrayName}_v[] = {{ {string.Join(", ", entries.Select(e => FormatDouble(e.Humidity!.Value)))} }};");
        sb.AppendLine($"static const int {arrayName}_len = {entries.Count};");
    }

    // "0.0###" ép LUÔN có ít nhất 1 chữ số thập phân (100 -> "100.0f", không
    // phải "100f") — C++ không chấp nhận literal float thiếu dấu chấm trước
    // hậu tố 'f' (xác nhận thật qua lỗi compile: "unable to find numeric
    // literal operator 'operator""f'" khi generate ra "100f").
    private static string FormatDouble(double value) => value.ToString("0.0###", CultureInfo.InvariantCulture) + "f";

    private static string SanitizeId(string id)
    {
        var chars = id.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray();
        return new string(chars);
    }

    private static string EscapeCString(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
