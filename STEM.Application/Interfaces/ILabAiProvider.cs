namespace STEM.Application.Interfaces;

/// <summary>
/// Provider abstraction cho AI Assistant trong Virtual Lab — handler nghiệp vụ
/// (LabAiAssistHandler) chỉ biết interface này, không biết provider cụ thể là ai,
/// base URL, API key, hay response shape (Anthropic Messages / OpenAI Chat Completions...).
/// </summary>
public interface ILabAiProvider
{
    /// <summary>True nếu provider đã có API key hợp lệ trong configuration — handler dùng
    /// để trả lỗi thân thiện TRƯỚC khi gọi ra ngoài, không cần biết giá trị key.</summary>
    bool IsConfigured { get; }

    Task<AiProviderCompletionResult> CompleteAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken = default);
}

/// <summary>Kết quả completion đã được provider chuẩn hoá về cùng 1 shape nội bộ.</summary>
public class AiProviderCompletionResult
{
    public required string Text { get; init; }
    public int PromptTokens { get; init; }
    public int CompletionTokens { get; init; }
}
