namespace STEM.Application.Dtos.Simulation;

// Sensor Input Bridge — Phase 1 (scenario/timeline, KHÔNG interactive realtime).
// Scenario được nhúng trong diagramJson (key "sensorScenario", ngang hàng với
// "parts"/"connections"/"board") — đi kèm sẵn qua đúng cơ chế save/reload
// diagram đã có, KHÔNG cần API/DB mới. Xem SensorRuntimeHeaderGenerator.cs
// (nơi DUY NHẤT tiêu thụ các DTO này để sinh C++ header) và
// VIRTUAL_LAB_PLAN.md mục "Sensor Input Bridge".
public sealed class SensorScenarioConfig
{
    // key = componentId (part.id trong diagram), KHÔNG phải componentType —
    // 2 HC-SR04 trên cùng 1 diagram phải cấu hình timeline riêng.
    public Dictionary<string, SensorTimeline> Sensors { get; set; } = new();
}

public sealed class SensorTimeline
{
    // componentType — chỉ để đối chiếu/log, KHÔNG dùng để quyết định field
    // nào áp dụng (field nào có giá trị thì áp dụng, xem
    // SensorRuntimeHeaderGenerator).
    public string Type { get; set; } = string.Empty;
    public List<SensorTimelineEntry> Timeline { get; set; } = new();
}

// Một entry = 1 mốc thời gian (kể từ lúc firmware boot, đối chiếu millis())
// + các giá trị sensor tại mốc đó. Field nào null = giữ nguyên giá trị mốc
// trước đó (step function), KHÔNG reset về default — cho phép 1 timeline chỉ
// cần khai báo field liên quan tới đúng loại sensor.
public sealed class SensorTimelineEntry
{
    public long TimeMs { get; set; }

    // HC-SR04 — khoảng cách cm, quy đổi pulseIn() duration bằng công thức
    // chuẩn 58us/cm (tốc độ âm thanh ~343m/s, khứ hồi).
    public double? DistanceCm { get; set; }

    // PIR Motion Sensor — digitalRead(OUT) trả HIGH khi true.
    public bool? Motion { get; set; }

    // Line Tracking 3ch/5ch (Phase 2.1) — 1 trong các giá trị:
    // "center","left","right","lost","intersection" (3ch) hoặc thêm
    // "far-left","far-right" (5ch). Pattern lạ/không khớp -> coi như "lost"
    // (an toàn, không suy đoán). Xem LineTrackingPatterns trong
    // SensorRuntimeHeaderGenerator.cs.
    public string? Pattern { get; set; }

    // Nhóm digital/analog chung (Phase 2.2) — Water Leak/Flame/Soil Moisture/
    // Rain/Vibration. Detected=false/true quyết định digitalRead HIGH/LOW
    // (xem GenericSensorPinConfig để biết pin nào ứng với sensor nào).
    // Analog (0-4095, tuỳ chọn) quyết định analogRead — nếu không set, suy
    // ra mặc định hợp lý từ Detected (false->thấp, true->cao).
    public bool? Detected { get; set; }
    public int? Analog { get; set; }

    // DHT11/DHT22 (Phase 2.3) — đọc qua StemFlowDHT helper (KHÔNG override
    // thư viện DHT thật), KHÔNG qua digitalRead/pulseIn.
    public double? Temperature { get; set; }
    public double? Humidity { get; set; }
}
