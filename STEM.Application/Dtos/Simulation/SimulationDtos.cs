using STEM.Core.Entities.Simulations;

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
    public string SourceCode { get; set; } = string.Empty;
    public string Board { get; set; } = "arduino:avr:uno";
    public string Framework { get; set; } = "arduino";

    // Optional — chỉ dùng để khoá cache incremental build theo từng project
    // (xem SimulationCompileService: core framework ESP32 chiếm ~86% thời
    // gian compile, gần như không đổi giữa các lần compile CÙNG 1 project).
    // Để trống (mặc định) = giữ nguyên hành vi cũ, không cache — AN TOÀN cho
    // mọi caller hiện có (BuildCompileCheckAsync lúc submit, endpoint
    // POST api/simulation/compile trực tiếp). CHỈ set field này ở caller nào
    // đã tự đảm bảo không có 2 compile đồng thời cho CÙNG projectId (đã xác
    // nhận thật: chia sẻ build-path giữa 2 compile ĐỒNG THỜI gây lẫn dữ liệu
    // — file .elf ra giống hệt nhau, sai với CẢ HAI sketch gốc). Hiện chỉ
    // QemuEsp32Runner set field này, vì IRunningSimulationRegistry đã đảm bảo
    // tại 1 thời điểm chỉ có tối đa 1 lần chạy nền (và do đó 1 lần compile)
    // cho mỗi projectId.
    public string? ProjectId { get; set; }
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
    public string? FirmwareBase64 { get; set; }
    public string? FirmwareFileName { get; set; }
    public string? FirmwareFormat { get; set; }
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

public class SaveDiagramRequest
{
    public Guid? LabId { get; set; }
    public string DiagramJson { get; set; } = string.Empty;
    public string? SourceCode { get; set; }
}

public class DiagramSessionResponse
{
    public string SessionId { get; set; } = string.Empty;
    public string DiagramJson { get; set; } = string.Empty;
    public DiagramValidationResponse Validation { get; set; } = new();
    public NetlistResponse Netlist { get; set; } = new();
    public DateTime UpdatedAt { get; set; }
}

public class DiagramValidationResponse
{
    public bool IsValid { get; set; }
    public IReadOnlyCollection<string> Errors { get; set; } = Array.Empty<string>();
    public IReadOnlyCollection<string> Warnings { get; set; } = Array.Empty<string>();
}

// Snapshot 1 lần (không realtime) cho Teacher "Xem live" — bổ sung phần
// SignalR-only còn thiếu: học sinh đã joined TRƯỚC khi giáo viên mở màn
// hình thì code/diagram/status hiện tại không tới qua event nào cả (chỉ có
// event cho THAY ĐỔI, không phải state hiện tại) — StudentSandboxViewer.tsx
// gọi API này 1 lần lúc mount để không bắt đầu từ rỗng, rồi vẫn nhận tiếp
// StudentCodeUpdated/StudentDiagramUpdated/... qua SignalR như bình thường.
public class TeacherProjectSnapshotResponse
{
    public string ProjectId { get; set; } = string.Empty;
    public int? StudentId { get; set; }
    public string Board { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string CodeContent { get; set; } = string.Empty;
    public string DiagramJson { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}

public class NetlistResponse
{
    public IReadOnlyCollection<NetResponse> Nets { get; set; } = Array.Empty<NetResponse>();
    public IReadOnlyDictionary<string, string> PinToNet { get; set; } = new Dictionary<string, string>();
}

public class NetResponse
{
    public string Id { get; set; } = string.Empty;
    public IReadOnlyCollection<string> Pins { get; set; } = Array.Empty<string>();
}

public class RunEsp32SimulationRequest
{
    public string? SessionId { get; set; }
    public Guid? LabId { get; set; }
    public int? AssignmentId { get; set; }
    public int? StudentId { get; set; }
    public int? ClassId { get; set; }
    public string DiagramJson { get; set; } = string.Empty;
    public string SourceCode { get; set; } = string.Empty;
    public string Mode { get; set; } = "mock";
}

public class RunEsp32SimulationResponse
{
    public string SessionId { get; set; } = string.Empty;
    public string Status { get; set; } = VirtualLabProjectStatuses.Running;
    public DiagramValidationResponse Validation { get; set; } = new();
    public NetlistResponse Netlist { get; set; } = new();
    public IReadOnlyCollection<SimulationEventResponse> Events { get; set; } = Array.Empty<SimulationEventResponse>();
}

public class SimulationEventResponse
{
    public string Type { get; set; } = string.Empty;
    public long Time { get; set; }
    public IReadOnlyDictionary<string, object?> Payload { get; set; } = new Dictionary<string, object?>();
}

public class Esp32CompileRequest
{
    public string Board { get; set; } = "esp32:esp32:esp32";
    public string Framework { get; set; } = "arduino";
    public string SourceCode { get; set; } = string.Empty;
    public int? AssignmentId { get; set; }
    public Guid? LabId { get; set; }
}

public class Esp32CompileResponse
{
    public bool Success { get; set; }
    public string? JobId { get; set; }
    public IReadOnlyCollection<string> Logs { get; set; } = Array.Empty<string>();
    public Esp32FirmwareResponse? Firmware { get; set; }
    public IReadOnlyCollection<CompileSimulationError> Errors { get; set; } = Array.Empty<CompileSimulationError>();
}

public class Esp32FirmwareResponse
{
    public string? BinPath { get; set; }
    public string? ElfPath { get; set; }
    public string? FileName { get; set; }
    public string? Format { get; set; }
    public string? Base64 { get; set; }
}

public class VirtualLabSubmissionRequest
{
    public int AssignmentId { get; set; }
    public string? SessionId { get; set; }
    public int? StudentId { get; set; }
    public string DiagramJson { get; set; } = string.Empty;
    public string SourceCode { get; set; } = string.Empty;
    public CompileSimulationResponse? CompileResult { get; set; }
    public IReadOnlyCollection<SimulationEventResponse> SimulationEvents { get; set; } = Array.Empty<SimulationEventResponse>();
}

public class VirtualLabSubmissionResponse
{
    public int SubmissionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? AutoScore { get; set; }
    public AutoGradeResultResponse AutoCheck { get; set; } = new();
}

public class AutoGradeResultResponse
{
    public bool Passed { get; set; }
    public int PassedChecks { get; set; }
    public int TotalChecks { get; set; }
    public IReadOnlyCollection<AutoGradeCheckResponse> Checks { get; set; } = Array.Empty<AutoGradeCheckResponse>();
}

public class AutoGradeCheckResponse
{
    public string Name { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string Message { get; set; } = string.Empty;
}
