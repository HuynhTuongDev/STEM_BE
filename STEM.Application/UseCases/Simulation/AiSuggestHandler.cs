using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Configuration;
using STEM.Application.Dtos.Simulation;
using STEM.Application.Interfaces;
using STEM.Core.Entities.Simulations;

namespace STEM.Application.UseCases.Simulation;

/// <summary>
/// Handler gọi Claude API để phân tích yêu cầu thí nghiệm từ ngôn ngữ tự nhiên,
/// tự động tạo WokwiDiagram và sinh Python code mẫu tương ứng.
/// </summary>
public class AiSuggestHandler
{
    private readonly IHttpClientFactory  _httpClientFactory;
    private readonly IWokwiService       _wokwiService;
    private readonly IConfiguration      _configuration;

    // Model theo project context — claude-sonnet-4-20250514
    private const string ClaudeModel    = "claude-sonnet-4-20250514";
    private const int    MaxTokens      = 1024;

    // Các component type được phép trong StemFlow sandbox
    private static readonly IReadOnlySet<string> _allowedTypes = new HashSet<string>
    {
        "wokwi-led", "wokwi-pushbutton", "wokwi-resistor",
        "wokwi-dht22", "wokwi-dht11", "wokwi-buzzer",
        "wokwi-lcd1602", "wokwi-potentiometer", "wokwi-servo"
    };

    public AiSuggestHandler(
        IHttpClientFactory  httpClientFactory,
        IWokwiService       wokwiService,
        IConfiguration      configuration)
    {
        _httpClientFactory = httpClientFactory;
        _wokwiService      = wokwiService;
        _configuration     = configuration;
    }

    public async Task<AiSuggestResponse> Handle(
        AiSuggestRequest  request,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate API key tồn tại
        var apiKey = _configuration["Anthropic:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Anthropic API key chưa được cấu hình (Anthropic:ApiKey).");
        }

        // 2. Gọi Claude API
        var claudeResponse = await CallClaudeAsync(request.Prompt, cancellationToken);

        // 3. Parse JSON trả về từ Claude
        var (summary, componentTypes, tip) = ParseClaudeResponse(claudeResponse);

        // 4. Lọc chỉ các type hợp lệ trong sandbox
        var validTypes = componentTypes
            .Where(t => _allowedTypes.Contains(t))
            .Distinct()
            .ToList();

        // 5. Build WokwiDiagram từ danh sách component type
        var diagram = BuildDiagram(validTypes);

        // 6. Sinh Python code và serialize diagram JSON
        var pythonCode  = _wokwiService.GeneratePythonCode(diagram);
        var diagramJson = _wokwiService.SerializeDiagram(diagram);

        return new AiSuggestResponse
        {
            Summary        = summary,
            ComponentTypes = validTypes,
            DiagramJson    = diagramJson,
            PythonCode     = pythonCode,
            Tip            = tip
        };
    }

    // ── Private: Gọi Claude Messages API ─────────────────────────────────────

    private async Task<string> CallClaudeAsync(
        string            userPrompt,
        CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient("Anthropic");

        var systemPrompt =
            """
            Bạn là trợ lý thiết kế thí nghiệm điện tử cho học sinh STEM.
            Khi nhận yêu cầu mô tả thí nghiệm, hãy trả lời DUY NHẤT bằng JSON hợp lệ (không giải thích, không markdown):
            {
              "summary": "<tóm tắt thí nghiệm bằng tiếng Việt, 1-2 câu>",
              "componentTypes": ["wokwi-led", "wokwi-resistor"],
              "tip": "<gợi ý học tập hoặc lưu ý an toàn bằng tiếng Việt>"
            }
            Chỉ được dùng các component sau (đúng tên):
            wokwi-led, wokwi-pushbutton, wokwi-resistor,
            wokwi-dht22, wokwi-dht11, wokwi-buzzer,
            wokwi-lcd1602, wokwi-potentiometer, wokwi-servo.
            componentTypes tối đa 6 phần tử, không trùng.
            """;

        var requestBody = new
        {
            model    = ClaudeModel,
            max_tokens = MaxTokens,
            system   = systemPrompt,
            messages = new[]
            {
                new { role = "user", content = userPrompt }
            }
        };

        var httpResponse = await httpClient.PostAsJsonAsync(
            "/v1/messages", requestBody, cancellationToken);

        if (!httpResponse.IsSuccessStatusCode)
        {
            var errorBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Claude API trả về lỗi {(int)httpResponse.StatusCode}: {errorBody}");
        }

        var rawJson = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

        // Lấy nội dung text từ content[0].text
        using var doc = JsonDocument.Parse(rawJson);
        var textContent = doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();

        return textContent
            ?? throw new InvalidOperationException("Claude API trả về content rỗng.");
    }

    // ── Private: Parse JSON từ Claude ────────────────────────────────────────

    private static (string Summary, List<string> ComponentTypes, string Tip)
        ParseClaudeResponse(string jsonText)
    {
        try
        {
            // Claude có thể bọc JSON trong ```json ... ``` — strip markdown nếu có
            var cleaned = jsonText.Trim();
            if (cleaned.StartsWith("```"))
            {
                var start = cleaned.IndexOf('\n') + 1;
                var end   = cleaned.LastIndexOf("```");
                cleaned   = cleaned[start..end].Trim();
            }

            using var doc = JsonDocument.Parse(cleaned);
            var root = doc.RootElement;

            var summary = root.TryGetProperty("summary", out var s)
                ? s.GetString() ?? string.Empty : string.Empty;

            var tip = root.TryGetProperty("tip", out var t)
                ? t.GetString() ?? string.Empty : string.Empty;

            var componentTypes = new List<string>();
            if (root.TryGetProperty("componentTypes", out var arr))
            {
                componentTypes = arr.EnumerateArray()
                    .Select(e => e.GetString() ?? string.Empty)
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .ToList();
            }

            return (summary, componentTypes, tip);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Không thể parse JSON từ Claude: {ex.Message}. Raw: {jsonText[..Math.Min(200, jsonText.Length)]}");
        }
    }

    // ── Private: Build WokwiDiagram từ danh sách type ────────────────────────

    private WokwiDiagram BuildDiagram(List<string> componentTypes)
    {
        // Lấy catalog để có DefaultAttrs
        var catalog = _wokwiService
            .GetAllComponents()
            .ToDictionary(c => c.Type, c => c);

        // Sắp xếp linh kiện theo hàng ngang, cách nhau 120px
        const double colStep = 120.0;
        const double topBase = 48.0;

        var parts = componentTypes
            .Select((type, index) =>
            {
                var shortName = type.Replace("wokwi-", string.Empty);
                var attrs     = catalog.TryGetValue(type, out var meta)
                                ? new Dictionary<string, string>(meta.DefaultAttrs)
                                : new Dictionary<string, string>();

                return new WokwiPart
                {
                    Id    = $"{shortName}{index + 1}",
                    Type  = type,
                    Left  = index * colStep,
                    Top   = topBase,
                    Attrs = attrs
                };
            })
            .ToList();

        return new WokwiDiagram
        {
            Version     = 1,
            Author      = "StemFlow AI",
            Editor      = "wokwi-2",
            Parts       = parts,
            Connections = new List<List<object>>()
        };
    }
}
