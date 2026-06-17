namespace STEM.Core.Entities.Simulations;

/// <summary>
/// Ánh xạ cấu trúc diagram.json của Wokwi.
/// Được lưu dưới dạng JSON string trong SimulationTemplate.Config.
/// </summary>
public class WokwiDiagram
{
    public int Version { get; set; }
    public string Author { get; set; } = string.Empty;
    public string Editor { get; set; } = string.Empty;
    public List<WokwiPart> Parts { get; set; } = new List<WokwiPart>();

    /// <summary>
    /// Mỗi connection là một mảng [pinA, pinB] hoặc [pinA, pinB, color, ...].
    /// Dùng List&lt;object&gt; để linh hoạt với các định dạng Wokwi khác nhau.
    /// </summary>
    public List<List<object>> Connections { get; set; } = new List<List<object>>();
}

/// <summary>
/// Đại diện cho một linh kiện điện tử trong sơ đồ Wokwi.
/// </summary>
public class WokwiPart
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public double Left { get; set; }
    public double Top { get; set; }
    public double Rotate { get; set; }

    /// <summary>
    /// Các thuộc tính bổ sung của linh kiện (ví dụ: color, value, label...).
    /// </summary>
    public Dictionary<string, string> Attrs { get; set; } = new Dictionary<string, string>();
}
