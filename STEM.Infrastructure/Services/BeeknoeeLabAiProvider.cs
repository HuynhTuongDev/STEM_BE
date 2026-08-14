using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using STEM.Application.Interfaces;

namespace STEM.Infrastructure.Services;

/// <summary>
/// ILabAiProvider implementation gọi Beeknoee Platform — OpenAI-compatible Chat Completions
/// API (POST {BaseUrl}/chat/completions, Authorization: Bearer {ApiKey}). Đây là điểm DUY NHẤT
/// biết về Beeknoee cụ thể (URL/key/response shape) — LabAiAssistHandler chỉ thấy ILabAiProvider.
/// </summary>
public class BeeknoeeLabAiProvider : ILabAiProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    private const string DefaultModel = "claude-sonnet-4-6";

    public BeeknoeeLabAiProvider(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_configuration["Beeknoee:ApiKey"]);

    public async Task<AiProviderCompletionResult> CompleteAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["Beeknoee:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Beeknoee API key chưa được cấu hình.");
        }

        var model = _configuration["Beeknoee:Model"];
        if (string.IsNullOrWhiteSpace(model)) model = DefaultModel;

        var requestBody = new
        {
            model,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMessage }
            }
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var rawJson = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

        if (!httpResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Beeknoee API trả về lỗi {(int)httpResponse.StatusCode}: {rawJson}");
        }

        using var doc = JsonDocument.Parse(rawJson);
        var root = doc.RootElement;

        var text = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()
            ?? throw new InvalidOperationException("Beeknoee API trả về content rỗng.");

        var promptTokens = 0;
        var completionTokens = 0;
        if (root.TryGetProperty("usage", out var usageEl))
        {
            if (usageEl.TryGetProperty("prompt_tokens", out var pt)) promptTokens = pt.GetInt32();
            if (usageEl.TryGetProperty("completion_tokens", out var ct)) completionTokens = ct.GetInt32();
        }

        return new AiProviderCompletionResult
        {
            Text = text,
            PromptTokens = promptTokens,
            CompletionTokens = completionTokens
        };
    }
}
