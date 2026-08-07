using System.Linq;
using System.Text.Json;

namespace STEM.Application.UseCases.Simulation.Runners.Qemu;


// Virtual Cloud Runtime — Phase 1 (StemFlowCloud). CÙNG kiến trúc với
// SensorRuntimeHeaderGenerator (không channel giao tiếp 2 chiều mới với
// QEMU, không WiFi thật — WiFi.begin/WiFi.mode gây Guru Meditation crash đã
// biết trong QEMU/ESP32 core 2.0.17, xem VIRTUAL_LAB_PLAN.md). cloud.publish()/
// cloud.log()/cloud.begin() chỉ IN RA SERIAL 1 dòng đánh dấu máy đọc được
// (SF_CLOUD_EVENT/SF_CLOUD_LOG {json}). QemuEsp32Runner đọc serial.log, parse
// JSON, emit SimulationEvent Type="cloud-event".
//
// BUG THẬT đã vá qua 2 vòng test thật trong QEMU (2026-07-29, KHÔNG suy đoán):
// 1) Bản đầu dùng ets_printf gọi trực tiếp từ method — QEMU reset lặp lại
//    ngay sau lần gọi đầu tiên trong cloud.begin().
// 2) Đổi sang Serial.print — VẪN crash, nhưng lần này thấy được nguyên văn:
//    "Guru Meditation Error: Core 1 panic'ed (Cache disabled but cached
//    memory region accessed)" — ĐÚNG loại lỗi "Cache error" không tất định đã
//    ghi nhận trước đây (xem GpioInstrumentationPreamble/
//    FirmwareCacheService.cs). Log cho thấy hành vi KHÔNG ổn định giữa các
//    lần thử (có lần in được "[cloud] cloud begin" rồi mới crash ở lệnh sau,
//    có lần crash ngay trước khi kịp in) — khớp mô tả "không tất định" gốc.
// Điểm chung của MỌI lần crash: lệnh in đầu tiên rơi đúng vào dòng NGAY SAU
// Serial.begin() trong setup() (demo code: "Serial.begin(115200);
// cloud.begin(...)", không có gì ở giữa) — khác các sketch Phase 1/2 đã test
// (luôn có vài lệnh pinMode/setup khác chạy trước khi print đầu tiên, cho hệ
// thống "ổn định" sau boot). Fix: thêm delay(50) ở ĐẦU begin() — lệnh in đầu
// tiên (dễ trúng cửa sổ cache-disabled nhất) — trước khi gọi ets_printf, né
// đúng cửa sổ thời điểm boot chưa ổn định. Giữ ets_printf (không phải Serial)
// cho publish()/log() vì đã proven an toàn qua digitalWrite (không phải lệnh
// in ĐẦU TIÊN trong chương trình, không cần delay tương tự).
//
// ets_printf ROM KHÔNG có tài liệu xác nhận hỗ trợ %f đáng tin (không suy
// đoán) — tự định dạng float bằng số nguyên (__sf_cloud_formatFloat), KHÔNG
// dùng %f, tránh luôn rủi ro printf-float bị newlib-nano strip.
//
// Sketch KHÔNG cần #include gì thêm — class StemFlowCloud đã có sẵn trong
// cùng file .ino lúc compile (giống StemFlowDHT ở SensorRuntimeHeaderGenerator).
public static class CloudRuntimeHeaderGenerator
{
    private const string CloudNodeType = "wokwi-wifi-cloud-node";
    private const string DashboardType = "wokwi-dashboard-cloud";

    private const string CloudRuntimeBlock = """
        // ---- StemFlow Virtual Cloud Runtime (Phase 1, auto-injected) ----
        // StemFlowCloud KHÔNG kết nối Internet/WiFi thật. begin()/publish()/log() chỉ in
        // 1 dòng đánh dấu máy đọc được ra Serial qua ets_printf — QemuEsp32Runner đọc
        // serial.log, parse JSON, emit SimulationEvent Type="cloud-event" (publish) hoặc
        // dòng Serial dễ đọc (begin/log). KHÔNG dùng WiFi.begin/WiFi.mode ở đây.
        extern "C" int ets_printf(const char *fmt, ...);
        static void __sf_cloud_formatFloat(char* buf, float value) {
          bool neg = value < 0;
          if (neg) value = -value;
          long whole = (long)value;
          int frac = (int)((value - (float)whole) * 100.0f + 0.5f);
          if (frac >= 100) { frac -= 100; whole += 1; }
          sprintf(buf, "%s%ld.%02d", neg ? "-" : "", whole, frac);
        }
        class StemFlowCloud {
          public:
            StemFlowCloud(const char* id) : _id(id) {}
            void begin(const char* label) {
              // delay(50) — lệnh in ĐẦU TIÊN của chương trình (demo code gọi ngay sau
              // Serial.begin(), không có lệnh nào ở giữa) rơi đúng cửa sổ boot chưa ổn
              // định gây "Cache disabled but cached memory region accessed" (xem comment
              // lớp trên) — publish()/log() KHÔNG cần delay này (không phải lệnh in đầu).
              delay(50);
              ets_printf("SF_CLOUD_LOG {\"componentId\":\"%s\",\"message\":\"cloud begin: %s\"}\n", _id, label);
            }
            void publish(const char* topic, float value) {
              char __sf_buf[32];
              __sf_cloud_formatFloat(__sf_buf, value);
              ets_printf("SF_CLOUD_EVENT {\"componentId\":\"%s\",\"topic\":\"%s\",\"value\":%s}\n", _id, topic, __sf_buf);
            }
            void publish(const char* topic, double value) { publish(topic, (float)value); }
            void publish(const char* topic, int value) {
              ets_printf("SF_CLOUD_EVENT {\"componentId\":\"%s\",\"topic\":\"%s\",\"value\":%d}\n", _id, topic, value);
            }
            void log(const char* message) {
              ets_printf("SF_CLOUD_LOG {\"componentId\":\"%s\",\"message\":\"%s\"}\n", _id, message);
            }
          private:
            const char* _id;
        };
        // ---- end StemFlow Virtual Cloud Runtime ----

        """;

    // Nhận diagramJson THÔ (giống TryParseScenario ở SensorRuntimeHeaderGenerator),
    // KHÔNG dùng VirtualLabRuntimeDiagramSnapshot — BUG THẬT đã vá (xác nhận qua
    // compile thật trong Docker sandbox, KHÔNG suy đoán): wokwi-wifi-cloud-node/
    // wokwi-dashboard-cloud CỐ TÌNH không có entry trong SupportedPins
    // (VirtualLabDiagramService.cs, đúng nguyên tắc "visual-only không vào
    // netlist") — nghĩa là BuildRuntimeComponents() luôn continue/bỏ qua 2 loại
    // này, snapshot.Components KHÔNG BAO GIỜ chứa chúng dù đã đặt trên canvas.
    // Gate theo snapshot ban đầu vì vậy luôn trả null — StemFlowCloud không bao
    // giờ được inject, "'StemFlowCloud' does not name a type" khi compile thật.
    // Đọc thẳng "parts[].type" từ diagramJson né hẳn bộ lọc netlist đó.
    //
    // Trả null nếu diagram không có component Cloud/Dashboard nào — giữ đúng
    // nguyên tắc cache-key-không-đổi của SensorRuntimeHeaderGenerator: diagram
    // không dùng Cloud thì phần header này rỗng, cache HIT như cũ.
    public static string? Generate(string diagramJson)
    {
        if (string.IsNullOrWhiteSpace(diagramJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(diagramJson);
            if (!document.RootElement.TryGetProperty("parts", out var partsEl) ||
                partsEl.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var hasCloudComponent = partsEl.EnumerateArray().Any(part =>
                part.TryGetProperty("type", out var typeEl) &&
                typeEl.ValueKind == JsonValueKind.String &&
                (string.Equals(typeEl.GetString(), CloudNodeType, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(typeEl.GetString(), DashboardType, StringComparison.OrdinalIgnoreCase)));

            return hasCloudComponent ? CloudRuntimeBlock : null;
        }
        catch (JsonException)
        {
            // diagramJson lỗi cú pháp đã bị VirtualLabDiagramService.Analyze() bắt
            // và báo lỗi riêng — ở đây chỉ coi như "không có Cloud component" thay
            // vì crash lần đọc thứ 2 này (giống TryParseScenario).
            return null;
        }
    }
}
