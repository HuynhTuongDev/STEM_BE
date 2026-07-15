namespace STEM.Application.Dtos.Simulation;

// ─────────────────────────────────────────────
// Template
// ─────────────────────────────────────────────

public class CreateTemplateRequest
{
    public string SimulationName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>Wokwi diagram.json dạng raw JSON string.</summary>
    public string DiagramJson { get; set; } = string.Empty;
}

public class UpdateDiagramRequest
{
    /// <summary>Wokwi diagram.json mới thay thế Config hiện tại.</summary>
    public string DiagramJson { get; set; } = string.Empty;
}

public class TemplateResponse
{
    public int Id { get; set; }
    public string SimulationName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DiagramJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// ─────────────────────────────────────────────
// Session
// ─────────────────────────────────────────────

public class CreateSessionRequest
{
    public int StudentId { get; set; }
    public int TemplateId { get; set; }
}

public class SessionResponse
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int TemplateId { get; set; }
    public DateTime StartTime { get; set; }

    /// <summary>Trạng thái phiên: "Active", "Completed", "Abandoned".</summary>
    public string Status { get; set; } = string.Empty;
}

// ─────────────────────────────────────────────
// AI Suggest
// ─────────────────────────────────────────────

public class AiSuggestRequest
{
    /// <summary>Mô tả thí nghiệm bằng ngôn ngữ tự nhiên (tiếng Việt hoặc tiếng Anh).</summary>
    public string Prompt { get; set; } = string.Empty;
}

public class AiSuggestResponse
{
    /// <summary>Tóm tắt thí nghiệm được đề xuất.</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>Danh sách Wokwi component type được sử dụng (ví dụ: ["wokwi-led", "wokwi-resistor"]).</summary>
    public List<string> ComponentTypes { get; set; } = new List<string>();

    /// <summary>Wokwi diagram.json do AI sinh ra, sẵn sàng load vào lab.</summary>
    public string DiagramJson { get; set; } = string.Empty;

    /// <summary>Python code mẫu (mock GPIO, không cần hardware thật).</summary>
    public string PythonCode { get; set; } = string.Empty;

    /// <summary>Gợi ý mở rộng hoặc lưu ý an toàn cho học sinh.</summary>
    public string Tip { get; set; } = string.Empty;
}

// ─────────────────────────────────────────────
// Component Catalog
// ─────────────────────────────────────────────

public class ComponentMetaResponse
{
    /// <summary>Wokwi component type, ví dụ: "wokwi-led".</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Tên hiển thị, ví dụ: "LED".</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Mô tả chức năng component.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Tất cả pin của component.</summary>
    public List<string> Pins { get; set; } = new List<string>();

    /// <summary>Các pin có thể kết nối GPIO.</summary>
    public List<string> GpioPins { get; set; } = new List<string>();

    public Dictionary<string, string> DefaultAttrs { get; set; } = new Dictionary<string, string>();
}

public class CompileSimulationRequest
{
    public int? AssignmentId { get; set; }
    public Guid? LabId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Board { get; set; } = "arduino:avr:uno";
}

public class CompileSimulationError
{
    public int? Line { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class CompileSimulationResponse
{
    public bool Success { get; set; }
    public string? JobId { get; set; }
    public string? HexBase64 { get; set; }
    public IReadOnlyCollection<CompileSimulationError> Errors { get; set; } = Array.Empty<CompileSimulationError>();
    public string? CompilerOutput { get; set; }
}

public class CompileJobResponse
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public CompileSimulationResponse? Result { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
