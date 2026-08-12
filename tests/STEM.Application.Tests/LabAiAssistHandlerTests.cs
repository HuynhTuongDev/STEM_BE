using Microsoft.Extensions.Configuration;
using STEM.Application.Dtos.Labs;
using STEM.Application.Dtos.Simulation;
using STEM.Application.Interfaces;
using STEM.Application.UseCases.Simulation;

namespace STEM.Application.Tests;

file sealed class FakeAiQuotaUsageStore : IAiQuotaUsageStore
{
    private readonly int _usedTokens;
    public int AddTodayUsageCallCount { get; private set; }
    public int LastAddedTokens { get; private set; }

    public FakeAiQuotaUsageStore(int usedTokens = 0) => _usedTokens = usedTokens;

    public Task<int> GetTodayUsedTokensAsync(int userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_usedTokens);

    public Task<int> AddTodayUsageAsync(int userId, int tokens, CancellationToken cancellationToken = default)
    {
        AddTodayUsageCallCount++;
        LastAddedTokens = tokens;
        return Task.FromResult(_usedTokens + tokens);
    }
}

file sealed class FakeLabService : ILabService
{
    private readonly IReadOnlyCollection<ComponentGlueRegistryResponse> _registry;
    public FakeLabService(IReadOnlyCollection<ComponentGlueRegistryResponse> registry) => _registry = registry;

    public Task<IReadOnlyCollection<ComponentGlueRegistryResponse>> GetComponentGlueRegistryAsync(
        bool supportedOnly = true, CancellationToken cancellationToken = default) =>
        Task.FromResult(_registry);

    public Task<PagedLabResponse> GetLabsAsync(GetLabsRequest request, int currentUserId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<LabResponse> GetLabAsync(Guid id, int currentUserId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<LabResponse> CreateLabAsync(CreateLabRequest request, int currentUserId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<LabResponse> UpdateLabAsync(Guid id, UpdateLabRequest request, int currentUserId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task DeleteLabAsync(Guid id, int currentUserId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<ValidateWokwiProjectResponse> ValidateWokwiProjectAsync(ValidateWokwiProjectRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<ValidateWokwiProjectResponse> ValidateExistingWokwiProjectAsync(Guid id, int currentUserId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<LabProgressResponse> StartProgressAsync(Guid id, int currentUserId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<LabProgressResponse> CompleteProgressAsync(Guid id, int currentUserId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<LabStatsResponse> GetStatsAsync(Guid id, int currentUserId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}

/// <summary>Fake ILabAiProvider — không gọi Beeknoee/Anthropic thật. Trả response đã cấu hình sẵn
/// hoặc ném exception đã cấu hình sẵn, và ghi lại có bị gọi hay không (để verify quota-exceeded /
/// missing-key path KHÔNG gọi ra ngoài).</summary>
file sealed class FakeLabAiProvider : ILabAiProvider
{
    private readonly AiProviderCompletionResult? _result;
    private readonly Exception? _exceptionToThrow;

    public bool IsConfigured { get; }
    public bool WasCalled { get; private set; }
    public string? LastSystemPrompt { get; private set; }
    public string? LastUserMessage { get; private set; }

    public FakeLabAiProvider(bool isConfigured, AiProviderCompletionResult? result = null, Exception? exceptionToThrow = null)
    {
        IsConfigured = isConfigured;
        _result = result;
        _exceptionToThrow = exceptionToThrow;
    }

    public Task<AiProviderCompletionResult> CompleteAsync(
        string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
    {
        WasCalled = true;
        LastSystemPrompt = systemPrompt;
        LastUserMessage = userMessage;
        if (_exceptionToThrow != null) throw _exceptionToThrow;
        return Task.FromResult(_result!);
    }
}

public class LabAiAssistHandlerTests
{
    private static IConfiguration MakeConfig(int? dailyLimit = null)
    {
        var dict = new Dictionary<string, string?>();
        if (dailyLimit != null) dict["Anthropic:DailyTokenLimit"] = dailyLimit.ToString();
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    private static ComponentGlueRegistryResponse MakeComponent(string type) => new() { ComponentType = type, Label = type, Supported = true };

    private const string ValidAiJsonResponse =
        """
        {"answer":"OK","requiresConfirmation":true,"confirmationMessage":"Áp dụng?","proposedChanges":[{"type":"replace_range","title":"Đổi delay","description":"500ms","before":"delay(1000);","after":"delay(500);","startLine":16,"endLine":16}]}
        """;

    [Fact]
    public async Task Handle_QuotaExceeded_DoesNotCallProvider_AndReturnsQuotaMessage()
    {
        var quotaStore = new FakeAiQuotaUsageStore(usedTokens: 50000);
        var provider = new FakeLabAiProvider(isConfigured: true, result: new AiProviderCompletionResult { Text = ValidAiJsonResponse, PromptTokens = 100, CompletionTokens = 50 });
        var handler = new LabAiAssistHandler(
            provider,
            MakeConfig(dailyLimit: 50000),
            quotaStore,
            new FakeLabService(Array.Empty<ComponentGlueRegistryResponse>()));

        var response = await handler.Handle(new LabAiAssistRequest { Prompt = "test" }, userId: 1);

        Assert.False(response.Success);
        Assert.Equal("daily_quota_exceeded", response.ErrorMessage);
        Assert.False(provider.WasCalled);
        Assert.Equal(0, quotaStore.AddTodayUsageCallCount);
    }

    [Fact]
    public async Task Handle_ProviderNotConfigured_DoesNotCallProvider_AndReturnsFriendlyMessage()
    {
        var quotaStore = new FakeAiQuotaUsageStore(usedTokens: 0);
        var provider = new FakeLabAiProvider(isConfigured: false);
        var handler = new LabAiAssistHandler(
            provider,
            MakeConfig(),
            quotaStore,
            new FakeLabService(Array.Empty<ComponentGlueRegistryResponse>()));

        var response = await handler.Handle(new LabAiAssistRequest { Prompt = "test" }, userId: 1);

        Assert.False(response.Success);
        Assert.Equal("missing_api_key", response.ErrorMessage);
        Assert.False(provider.WasCalled);
        Assert.Equal(0, quotaStore.AddTodayUsageCallCount);
    }

    [Fact]
    public async Task Handle_QuotaAvailable_ValidResponse_ParsesProposedChanges_AndIncrementsQuota()
    {
        var quotaStore = new FakeAiQuotaUsageStore(usedTokens: 100);
        var provider = new FakeLabAiProvider(isConfigured: true, result: new AiProviderCompletionResult { Text = ValidAiJsonResponse, PromptTokens = 100, CompletionTokens = 50 });
        var handler = new LabAiAssistHandler(
            provider,
            MakeConfig(dailyLimit: 50000),
            quotaStore,
            new FakeLabService(Array.Empty<ComponentGlueRegistryResponse>()));

        var response = await handler.Handle(new LabAiAssistRequest { Prompt = "Đổi delay" }, userId: 1);

        Assert.True(response.Success);
        Assert.True(provider.WasCalled);
        Assert.Single(response.ProposedChanges);
        Assert.Equal(ProposedChangeTypes.ReplaceRange, response.ProposedChanges[0].Type);
        Assert.Equal(1, quotaStore.AddTodayUsageCallCount);
        Assert.Equal(150, quotaStore.LastAddedTokens); // 100 prompt + 50 completion — mapped từ usage.prompt_tokens/completion_tokens của provider
        Assert.NotNull(response.Usage);
        Assert.Equal(150, response.Usage!.TotalTokens);
    }

    [Fact]
    public async Task Handle_UnsupportedComponentType_IsFilteredOut()
    {
        const string responseWithBadComponent =
            """
            {"answer":"OK","requiresConfirmation":true,"confirmationMessage":"Áp dụng?","proposedChanges":[{"type":"add_component","title":"Thêm cảm biến","description":"x","component":{"type":"wokwi-invented-sensor-9000","id":"s1","x":10,"y":10}}]}
            """;
        var quotaStore = new FakeAiQuotaUsageStore();
        var provider = new FakeLabAiProvider(isConfigured: true, result: new AiProviderCompletionResult { Text = responseWithBadComponent, PromptTokens = 10, CompletionTokens = 10 });
        var handler = new LabAiAssistHandler(
            provider,
            MakeConfig(dailyLimit: 50000),
            quotaStore,
            new FakeLabService(new[] { MakeComponent("wokwi-led"), MakeComponent("wokwi-buzzer") }));

        var response = await handler.Handle(new LabAiAssistRequest { Prompt = "Thêm cảm biến" }, userId: 1);

        Assert.True(response.Success);
        Assert.Empty(response.ProposedChanges);
    }

    [Fact]
    public async Task Handle_SupportedComponentType_IsKept()
    {
        const string responseWithGoodComponent =
            """
            {"answer":"OK","requiresConfirmation":true,"confirmationMessage":"Áp dụng?","proposedChanges":[{"type":"add_component","title":"Thêm LED","description":"x","component":{"type":"wokwi-led","id":"led2","x":10,"y":10}}]}
            """;
        var quotaStore = new FakeAiQuotaUsageStore();
        var provider = new FakeLabAiProvider(isConfigured: true, result: new AiProviderCompletionResult { Text = responseWithGoodComponent, PromptTokens = 10, CompletionTokens = 10 });
        var handler = new LabAiAssistHandler(
            provider,
            MakeConfig(dailyLimit: 50000),
            quotaStore,
            new FakeLabService(new[] { MakeComponent("wokwi-led"), MakeComponent("wokwi-buzzer") }));

        var response = await handler.Handle(new LabAiAssistRequest { Prompt = "Thêm LED" }, userId: 1);

        Assert.True(response.Success);
        Assert.Single(response.ProposedChanges);
        Assert.Equal("wokwi-led", response.ProposedChanges[0].Component?.Type);
    }

    [Fact]
    public async Task Handle_ProviderThrows401_ReturnsFriendlyMessage_NeverExposesApiKey()
    {
        const string apiKey = "sk-bee-super-secret-value";
        var quotaStore = new FakeAiQuotaUsageStore();
        var provider = new FakeLabAiProvider(
            isConfigured: true,
            exceptionToThrow: new InvalidOperationException(
                """Beeknoee API trả về lỗi 401: {"error":{"message":"invalid api key"}}"""));
        var handler = new LabAiAssistHandler(
            provider,
            MakeConfig(dailyLimit: 50000),
            quotaStore,
            new FakeLabService(Array.Empty<ComponentGlueRegistryResponse>()));

        var response = await handler.Handle(new LabAiAssistRequest { Prompt = "test" }, userId: 1);

        Assert.False(response.Success);
        Assert.DoesNotContain(apiKey, response.Answer);
        Assert.DoesNotContain(apiKey, response.ErrorMessage ?? string.Empty);
        Assert.Equal(0, quotaStore.AddTodayUsageCallCount);
    }

    [Fact]
    public async Task Handle_ProviderTimesOut_ReturnsFriendlyMessage_DoesNotIncrementQuota()
    {
        var quotaStore = new FakeAiQuotaUsageStore();
        var provider = new FakeLabAiProvider(
            isConfigured: true,
            exceptionToThrow: new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout"));
        var handler = new LabAiAssistHandler(
            provider,
            MakeConfig(dailyLimit: 50000),
            quotaStore,
            new FakeLabService(Array.Empty<ComponentGlueRegistryResponse>()));

        var response = await handler.Handle(new LabAiAssistRequest { Prompt = "test" }, userId: 1);

        Assert.False(response.Success);
        Assert.Equal("Không thể kết nối tới AI lúc này, vui lòng thử lại sau.", response.Answer);
        Assert.Equal(0, quotaStore.AddTodayUsageCallCount);
    }

    [Fact]
    public async Task Handle_MalformedJsonFromProvider_FallsBackToPlainTextAnswer_NoChanges()
    {
        var quotaStore = new FakeAiQuotaUsageStore();
        var provider = new FakeLabAiProvider(isConfigured: true, result: new AiProviderCompletionResult { Text = "Xin chào, đây không phải JSON hợp lệ {{{", PromptTokens = 5, CompletionTokens = 5 });
        var handler = new LabAiAssistHandler(
            provider,
            MakeConfig(dailyLimit: 50000),
            quotaStore,
            new FakeLabService(Array.Empty<ComponentGlueRegistryResponse>()));

        var response = await handler.Handle(new LabAiAssistRequest { Prompt = "test" }, userId: 1);

        Assert.True(response.Success);
        Assert.Empty(response.ProposedChanges);
        Assert.Equal(1, quotaStore.AddTodayUsageCallCount);
    }
}
