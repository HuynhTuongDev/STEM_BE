using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using STEM.Application.Dtos.Simulation;
using STEM.Application.Interfaces;

namespace STEM.Application.UseCases.Simulation;

/// <summary>
/// AI Assistant trong phòng Lab — workflow đề xuất-rồi-xác-nhận kiểu Codex/Cursor:
/// AI chỉ trả về proposedChanges, KHÔNG bao giờ tự sửa code/diagram. FE chịu trách
/// nhiệm áp dụng sau khi người dùng bấm Apply. Khác với AiSuggestHandler (sinh mạch
/// mới từ đầu theo prompt) — handler này review code/diagram ĐANG có trong project.
/// </summary>
public class LabAiAssistHandler
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IAiQuotaUsageStore _quotaStore;

    private const string ClaudeModel = "claude-sonnet-4-20250514";
    private const int MaxTokens = 3072;
    private const int DefaultDailyTokenLimit = 50000;

    public LabAiAssistHandler(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IAiQuotaUsageStore quotaStore)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _quotaStore = quotaStore;
    }

    public async Task<LabAiAssistResponse> Handle(
        LabAiAssistRequest request,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var dailyLimit = _configuration.GetValue<int?>("Anthropic:DailyTokenLimit") ?? DefaultDailyTokenLimit;
        var usedSoFar = await _quotaStore.GetTodayUsedTokensAsync(userId, cancellationToken);

        if (usedSoFar >= dailyLimit)
        {
            return QuotaExceededResponse(usedSoFar, dailyLimit);
        }

        var apiKey = _configuration["Anthropic:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new LabAiAssistResponse
            {
                Success = false,
                Answer = "Tính năng AI Assistant chưa được cấu hình API key ở server. Vui lòng liên hệ quản trị viên.",
                RequiresConfirmation = false,
                ProposedChanges = new List<ProposedChangeDto>(),
                ErrorMessage = "missing_api_key"
            };
        }

        string rawText;
        int promptTokens;
        int completionTokens;
        try
        {
            (rawText, promptTokens, completionTokens) = await CallClaudeAsync(request, apiKey, cancellationToken);
        }
        catch (Exception ex)
        {
            return new LabAiAssistResponse
            {
                Success = false,
                Answer = "Không thể kết nối tới AI lúc này, vui lòng thử lại sau.",
                RequiresConfirmation = false,
                ProposedChanges = new List<ProposedChangeDto>(),
                ErrorMessage = ex.Message
            };
        }

        var parsed = ParseClaudeResponse(rawText);

        var totalTokensThisCall = promptTokens + completionTokens;
        var newDailyTotal = await _quotaStore.AddTodayUsageAsync(userId, totalTokensThisCall, cancellationToken);

        parsed.Success = true;
        parsed.Usage = new AiUsageDto
        {
            PromptTokens = promptTokens,
            CompletionTokens = completionTokens,
            TotalTokens = totalTokensThisCall,
            DailyUsedTokens = newDailyTotal,
            DailyLimitTokens = dailyLimit,
            RemainingDailyTokens = Math.Max(0, dailyLimit - newDailyTotal),
            IsEstimated = false
        };

        return parsed;
    }

    private static LabAiAssistResponse QuotaExceededResponse(int usedSoFar, int dailyLimit)
    {
        return new LabAiAssistResponse
        {
            Success = false,
            Answer = "Bạn đã dùng hết hạn mức token AI cho hôm nay. Vui lòng thử lại vào ngày mai.",
            RequiresConfirmation = false,
            ProposedChanges = new List<ProposedChangeDto>(),
            ErrorMessage = "daily_quota_exceeded",
            Usage = new AiUsageDto
            {
                PromptTokens = 0,
                CompletionTokens = 0,
                TotalTokens = 0,
                DailyUsedTokens = usedSoFar,
                DailyLimitTokens = dailyLimit,
                RemainingDailyTokens = Math.Max(0, dailyLimit - usedSoFar),
                IsEstimated = false
            }
        };
    }

    // ── Gọi Claude Messages API ──────────────────────────────────────────────

    private async Task<(string Text, int PromptTokens, int CompletionTokens)> CallClaudeAsync(
        LabAiAssistRequest request,
        string apiKey,
        CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient("Anthropic");

        var userContent = BuildUserMessage(request);

        var requestBody = new
        {
            model = ClaudeModel,
            max_tokens = MaxTokens,
            system = SystemPrompt,
            messages = new[]
            {
                new { role = "user", content = userContent }
            }
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/messages");
        httpRequest.Headers.Add("x-api-key", apiKey);
        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var httpResponse = await httpClient.SendAsync(httpRequest, cancellationToken);

        var rawJson = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

        if (!httpResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Claude API trả về lỗi {(int)httpResponse.StatusCode}: {rawJson}");
        }

        using var doc = JsonDocument.Parse(rawJson);
        var root = doc.RootElement;

        var text = root.GetProperty("content")[0].GetProperty("text").GetString()
            ?? throw new InvalidOperationException("Claude API trả về content rỗng.");

        var promptTokens = 0;
        var completionTokens = 0;
        if (root.TryGetProperty("usage", out var usageEl))
        {
            if (usageEl.TryGetProperty("input_tokens", out var inTok)) promptTokens = inTok.GetInt32();
            if (usageEl.TryGetProperty("output_tokens", out var outTok)) completionTokens = outTok.GetInt32();
        }

        return (text, promptTokens, completionTokens);
    }

    private static string BuildUserMessage(LabAiAssistRequest request)
    {
        var diagram = string.IsNullOrWhiteSpace(request.CurrentDiagramJson) ? "(không có)" : request.CurrentDiagramJson;
        var code = string.IsNullOrWhiteSpace(request.CurrentCode) ? "(chưa có code)" : request.CurrentCode;

        return $"""
            BỐI CẢNH — CODE HIỆN TẠI (sketch.ino):
            ```
            {code}
            ```

            BỐI CẢNH — DIAGRAM JSON HIỆN TẠI:
            ```
            {diagram}
            ```

            YÊU CẦU CỦA NGƯỜI DÙNG:
            {request.Prompt}
            """;
    }

    private const string SystemPrompt =
        """
        Bạn là trợ lý lập trình AI trong "STEM Virtual Lab" — phòng thí nghiệm ảo mô phỏng vi
        điều khiển ESP32 bằng Arduino C++ (arduino-cli) chạy trên QEMU. Học sinh/giáo viên hỏi
        bạn về code hoặc sơ đồ mạch (diagram) đang mở trong Sandbox.

        QUY TẮC BẮT BUỘC — KHÔNG ĐƯỢC VI PHẠM:
        1. Bạn KHÔNG có khả năng tự thay đổi code, diagram hay linh kiện. Bạn chỉ ĐỀ XUẤT; người
           dùng phải tự bấm "Áp dụng" trên giao diện thì thay đổi mới thực sự xảy ra.
        2. TUYỆT ĐỐI không dùng các câu khẳng định như "Mình đã sửa xong", "Đã cập nhật code",
           "Đã thêm linh kiện" — vì bạn CHƯA áp dụng gì cả. Luôn dùng lối nói đề xuất: "Mình đề
           xuất...", kết thúc bằng câu hỏi xác nhận kiểu "Bạn có muốn áp dụng thay đổi này không?".
        3. Nếu yêu cầu chỉ cần giải thích/trả lời kiến thức (không cần sửa code/diagram), trả về
           requiresConfirmation=false và proposedChanges=[] (mảng rỗng) — KHÔNG bịa ra thay đổi.
        4. Không tự bịa loại linh kiện mới — chỉ dùng type bắt đầu bằng "wokwi-" đã tồn tại trong
           hệ thống (wokwi-led, wokwi-buzzer, wokwi-pushbutton, wokwi-servo, wokwi-rgb-led,
           wokwi-dht11, wokwi-dht22, wokwi-hc-sr04, wokwi-l298n, wokwi-dc-motor,
           wokwi-pir-motion-sensor, wokwi-rain-sensor, wokwi-soil-moisture-sensor,
           wokwi-water-leak-sensor, wokwi-flame-sensor, wokwi-vibration-sensor,
           wokwi-line-tracking-3ch, wokwi-line-tracking-5ch, v.v.).
        5. Board/ESP32 trong connections LUÔN dùng id cố định "arduino" (không dùng "esp32").
           Token nối dây có dạng "partId:pinName", ví dụ "arduino:GPIO18", "led1:A".
        6. Trả lời bằng tiếng Việt.

        Khi cần đề xuất thay đổi, PHẢI trả lời DUY NHẤT bằng JSON hợp lệ theo đúng cấu trúc sau
        (không thêm chữ nào ngoài JSON, không bọc markdown ```):
        {
          "answer": "<câu trả lời/giải thích bằng tiếng Việt, theo lối đề xuất, không khẳng định đã làm>",
          "requiresConfirmation": true hoặc false,
          "confirmationMessage": "<câu hỏi xác nhận ngắn — bắt buộc khi requiresConfirmation=true, để chuỗi rỗng nếu false>",
          "proposedChanges": [
            {
              "type": "replace_file" | "replace_range" | "insert_code" | "add_component" | "update_wire" | "update_diagram_json" | "explanation_only",
              "title": "<tiêu đề ngắn>",
              "description": "<mô tả thay đổi làm gì và tại sao>",
              "before": "<đoạn code hiện tại liên quan — dùng cho replace_file/replace_range, để trống nếu không áp dụng>",
              "after": "<đoạn code mới — dùng cho replace_file/replace_range/insert_code>",
              "startLine": <số dòng bắt đầu 1-based, chỉ dùng cho replace_range>,
              "endLine": <số dòng kết thúc 1-based, chỉ dùng cho replace_range>,
              "insertAtLine": <vị trí chèn 1-based, chỉ dùng cho insert_code, bỏ qua nếu chèn cuối file>,
              "component": { "type": "wokwi-led", "id": "led2", "x": 200, "y": 100 },
              "wire": { "from": "led2:A", "to": "arduino:GPIO18", "color": "red" },
              "diagramJson": "<toàn bộ diagram JSON mới — chỉ dùng cho update_diagram_json>"
            }
          ]
        }
        Nếu không có đề xuất thay đổi nào (chỉ trả lời câu hỏi), proposedChanges phải là mảng rỗng [].
        """;

    // ── Parse JSON từ Claude ──────────────────────────────────────────────────

    private static LabAiAssistResponse ParseClaudeResponse(string jsonText)
    {
        var cleaned = jsonText.Trim();
        if (cleaned.StartsWith("```"))
        {
            var start = cleaned.IndexOf('\n') + 1;
            var end = cleaned.LastIndexOf("```");
            if (end > start)
            {
                cleaned = cleaned[start..end].Trim();
            }
        }

        try
        {
            using var doc = JsonDocument.Parse(cleaned);
            var root = doc.RootElement;

            var answer = root.TryGetProperty("answer", out var a) ? a.GetString() ?? string.Empty : cleaned;
            var requiresConfirmation = root.TryGetProperty("requiresConfirmation", out var rc)
                && rc.ValueKind == JsonValueKind.True;
            var confirmationMessage = root.TryGetProperty("confirmationMessage", out var cm)
                ? cm.GetString() : null;

            var changes = new List<ProposedChangeDto>();
            if (root.TryGetProperty("proposedChanges", out var changesEl) && changesEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in changesEl.EnumerateArray())
                {
                    var change = ParseProposedChange(item);
                    if (change != null) changes.Add(change);
                }
            }

            // Ràng buộc phòng thủ: nếu model quên set field, tự suy ra cho nhất quán.
            if (changes.Count == 0)
            {
                requiresConfirmation = false;
                confirmationMessage = null;
            }
            else if (requiresConfirmation && string.IsNullOrWhiteSpace(confirmationMessage))
            {
                confirmationMessage = "Bạn có muốn áp dụng thay đổi này không?";
            }

            return new LabAiAssistResponse
            {
                Answer = answer,
                RequiresConfirmation = requiresConfirmation,
                ConfirmationMessage = confirmationMessage,
                ProposedChanges = changes
            };
        }
        catch (JsonException)
        {
            // Model trả lời dạng văn bản tự do thay vì JSON — coi như câu trả lời thuần,
            // không có đề xuất thay đổi (an toàn hơn là báo lỗi toàn bộ request).
            return new LabAiAssistResponse
            {
                Answer = jsonText.Trim(),
                RequiresConfirmation = false,
                ConfirmationMessage = null,
                ProposedChanges = new List<ProposedChangeDto>()
            };
        }
    }

    private static ProposedChangeDto? ParseProposedChange(JsonElement item)
    {
        if (item.ValueKind != JsonValueKind.Object) return null;

        var type = item.TryGetProperty("type", out var t) ? t.GetString() ?? string.Empty : string.Empty;
        if (!ProposedChangeTypes.All.Contains(type)) return null;

        var dto = new ProposedChangeDto
        {
            Id = Guid.NewGuid().ToString("N"),
            Type = type,
            Title = item.TryGetProperty("title", out var ti) ? ti.GetString() ?? string.Empty : string.Empty,
            Description = item.TryGetProperty("description", out var de) ? de.GetString() ?? string.Empty : string.Empty,
            Before = item.TryGetProperty("before", out var be) ? be.GetString() : null,
            After = item.TryGetProperty("after", out var af) ? af.GetString() : null,
            StartLine = item.TryGetProperty("startLine", out var sl) && sl.ValueKind == JsonValueKind.Number ? sl.GetInt32() : null,
            EndLine = item.TryGetProperty("endLine", out var el) && el.ValueKind == JsonValueKind.Number ? el.GetInt32() : null,
            InsertAtLine = item.TryGetProperty("insertAtLine", out var ia) && ia.ValueKind == JsonValueKind.Number ? ia.GetInt32() : null,
            DiagramJson = item.TryGetProperty("diagramJson", out var dj) ? dj.GetString() : null
        };

        if (item.TryGetProperty("component", out var comp) && comp.ValueKind == JsonValueKind.Object)
        {
            dto.Component = new AiComponentSuggestionDto
            {
                Type = comp.TryGetProperty("type", out var ct) ? ct.GetString() ?? string.Empty : string.Empty,
                Id = comp.TryGetProperty("id", out var ci) ? ci.GetString() : null,
                X = comp.TryGetProperty("x", out var cx) && cx.ValueKind == JsonValueKind.Number ? cx.GetDouble() : null,
                Y = comp.TryGetProperty("y", out var cy) && cy.ValueKind == JsonValueKind.Number ? cy.GetDouble() : null
            };
        }

        if (item.TryGetProperty("wire", out var wire) && wire.ValueKind == JsonValueKind.Object)
        {
            dto.Wire = new AiWireSuggestionDto
            {
                From = wire.TryGetProperty("from", out var wf) ? wf.GetString() ?? string.Empty : string.Empty,
                To = wire.TryGetProperty("to", out var wt) ? wt.GetString() ?? string.Empty : string.Empty,
                Color = wire.TryGetProperty("color", out var wc) ? wc.GetString() : null
            };
        }

        return dto;
    }
}
