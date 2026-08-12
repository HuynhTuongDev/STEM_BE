namespace STEM.Application.Dtos.Simulation;

/// <summary>
/// Các type hợp lệ của ProposedChangeDto.Type — khớp với FE AiProposedChangesPanel.
/// </summary>
public static class ProposedChangeTypes
{
    public const string ReplaceFile = "replace_file";
    public const string ReplaceRange = "replace_range";
    public const string InsertCode = "insert_code";
    public const string AddComponent = "add_component";
    public const string UpdateWire = "update_wire";
    public const string UpdateDiagramJson = "update_diagram_json";
    public const string ExplanationOnly = "explanation_only";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        ReplaceFile, ReplaceRange, InsertCode, AddComponent, UpdateWire, UpdateDiagramJson, ExplanationOnly
    };
}

public class LabAiAssistRequest
{
    /// <summary>Câu hỏi/yêu cầu của người dùng gửi cho AI.</summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>Nội dung sketch.ino hiện tại trên editor (context cho AI, KHÔNG phải để AI tự sửa).</summary>
    public string CurrentCode { get; set; } = string.Empty;

    /// <summary>Diagram JSON hiện tại (LabCircuitConfig serialize) — context cho AI.</summary>
    public string? CurrentDiagramJson { get; set; }
}

/// <summary>Gợi ý thêm 1 linh kiện mới vào canvas — chỉ áp dụng khi FE hỗ trợ add-component và người dùng xác nhận.</summary>
public class AiComponentSuggestionDto
{
    public string Type { get; set; } = string.Empty;
    public string? Id { get; set; }
    public double? X { get; set; }
    public double? Y { get; set; }
}

/// <summary>Gợi ý nối dây giữa 2 chân — token dạng "partId:pinName" (khớp format connections hiện có).</summary>
public class AiWireSuggestionDto
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string? Color { get; set; }
}

/// <summary>
/// 1 đề xuất thay đổi — KHÔNG được FE tự áp dụng, chỉ hiển thị preview chờ người dùng Apply/Reject.
/// </summary>
public class ProposedChangeDto
{
    /// <summary>Id duy nhất trong phạm vi 1 response, sinh ở server (Guid rút gọn).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>1 trong 7 giá trị của ProposedChangeTypes.</summary>
    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // ── replace_file / replace_range / insert_code ──
    /// <summary>Nội dung code TRƯỚC khi đổi — dùng để FE phát hiện conflict (so với code hiện tại lúc Apply).</summary>
    public string? Before { get; set; }
    /// <summary>Nội dung code SAU khi đổi (đoạn thay thế/chèn).</summary>
    public string? After { get; set; }
    /// <summary>Dòng bắt đầu (1-based) — dùng cho replace_range.</summary>
    public int? StartLine { get; set; }
    /// <summary>Dòng kết thúc (1-based, inclusive) — dùng cho replace_range.</summary>
    public int? EndLine { get; set; }
    /// <summary>Vị trí chèn (1-based) — dùng cho insert_code. Bỏ trống = chèn cuối file.</summary>
    public int? InsertAtLine { get; set; }

    // ── add_component / update_wire ──
    public AiComponentSuggestionDto? Component { get; set; }
    public AiWireSuggestionDto? Wire { get; set; }

    // ── update_diagram_json ──
    /// <summary>Diagram JSON đầy đủ đề xuất thay thế toàn bộ sơ đồ hiện tại.</summary>
    public string? DiagramJson { get; set; }
}

public class AiUsageDto
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }

    /// <summary>Tổng token đã dùng trong ngày (UTC), TÍNH CẢ lần gọi này.</summary>
    public int DailyUsedTokens { get; set; }
    public int DailyLimitTokens { get; set; }
    public int RemainingDailyTokens { get; set; }

    /// <summary>True nếu số liệu là ước lượng (Claude không trả usage thật) thay vì số chính xác từ provider.</summary>
    public bool IsEstimated { get; set; }
}

public class LabAiAssistResponse
{
    public bool Success { get; set; }

    /// <summary>Câu trả lời/giải thích bằng tiếng Việt hiển thị trực tiếp cho người dùng.</summary>
    public string Answer { get; set; } = string.Empty;

    /// <summary>True nếu có ProposedChanges cần người dùng xác nhận trước khi áp dụng.</summary>
    public bool RequiresConfirmation { get; set; }

    /// <summary>Câu hỏi xác nhận, ví dụ "Bạn có muốn áp dụng thay đổi này không?" — bắt buộc có khi RequiresConfirmation=true.</summary>
    public string? ConfirmationMessage { get; set; }

    public List<ProposedChangeDto> ProposedChanges { get; set; } = new();

    public AiUsageDto? Usage { get; set; }

    /// <summary>Thông báo lỗi (vd. hết quota, lỗi gọi provider) — chỉ có giá trị khi Success=false.</summary>
    public string? ErrorMessage { get; set; }
}
