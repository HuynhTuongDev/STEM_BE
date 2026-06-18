using STEM.Core.Entities.Simulations;

namespace STEM.Application.Interfaces;

/// <summary>
/// Service xử lý toàn bộ logic liên quan đến Wokwi diagram:
/// parse/serialize JSON, validate cấu trúc, generate Python code, liệt kê component.
/// </summary>
public interface IWokwiService
{
    /// <summary>
    /// Parse JSON string (diagram.json format của Wokwi) thành WokwiDiagram object.
    /// </summary>
    WokwiDiagram ParseDiagram(string json);

    /// <summary>
    /// Serialize WokwiDiagram object thành JSON string để lưu vào SimulationTemplate.Config.
    /// </summary>
    string SerializeDiagram(WokwiDiagram diagram);

    /// <summary>
    /// Kiểm tra tính hợp lệ của diagram: component type có hỗ trợ không, connection có đủ pin không, v.v.
    /// Trả về tuple (IsValid, danh sách lỗi nếu có).
    /// </summary>
    (bool IsValid, List<string> Errors) ValidateDiagram(WokwiDiagram diagram);

    /// <summary>
    /// Sinh Python code mẫu từ diagram, mock RPi.GPIO / Adafruit_DHT — không dùng hardware thật.
    /// </summary>
    string GeneratePythonCode(WokwiDiagram diagram);

    /// <summary>
    /// Trả về danh sách tất cả component được hỗ trợ trong sandbox (wokwi-led, wokwi-dht22, ...).
    /// </summary>
    IEnumerable<ComponentMeta> GetAllComponents();
}

/// <summary>
/// Metadata mô tả một loại component Wokwi được hỗ trợ trong StemFlow sandbox.
/// </summary>
public class ComponentMeta
{
    /// <summary>Wokwi component type, ví dụ: "wokwi-led", "wokwi-dht22".</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Tên hiển thị thân thiện, ví dụ: "LED", "DHT22 Sensor".</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Mô tả chức năng của component.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Tất cả tên pin của component (ví dụ: ["A", "C"]).</summary>
    public List<string> Pins { get; set; } = new List<string>();

    /// <summary>Các pin GPIO có thể kết nối với Raspberry Pi mock (ví dụ: ["A"]).</summary>
    public List<string> GpioPins { get; set; } = new List<string>();

    /// <summary>
    /// Thuộc tính mặc định khi thêm component vào diagram
    /// (ví dụ: color="red" cho LED, value="220" cho resistor).
    /// </summary>
    public Dictionary<string, string> DefaultAttrs { get; set; } = new Dictionary<string, string>();
}
