using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STEM.Application.Dtos.Simulation;
using STEM.Application.UseCases.Simulation;
using STEM.Application.UseCases.Simulation.Abstractions;
using STEM.Application.UseCases.Simulation.Runners.Educational;
using STEM.Application.UseCases.Simulation.Runners.Mock;
using STEM.Application.UseCases.Simulation.Runtime;
using STEM.Core.Entities.Simulations;

namespace STEM.Application.Tests;

public sealed class SimulationRunnerResolverTests
{
    [Fact]
    public void Resolve_ReturnsMockRunner_WhenModeIsMock()
    {
        var configuration = new ConfigurationManager();
        configuration["SimulationRunner:MaxConcurrentRuns"] = "10";

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<VirtualLabDiagramService>();
        services.AddSingleton<EducationalProgramAnalyzer>();
        services.AddSingleton<EducationalEventGenerator>();
        services.AddSingleton<VirtualLabMockRunner>();
        services.AddSingleton<ISimulationEventBroadcaster, NoOpSimulationEventBroadcaster>();
        services.AddSingleton<IRunningSimulationRegistry, RunningSimulationRegistry>();
        services.AddSingleton<ISimulationInputChannel, SimulationInputChannel>();
        services.AddScoped<ISimulationEventStore, FakeSimulationEventStore>();
        services.AddSingleton<EducationalSimulationRunner>();
        services.AddScoped<ISimulationRunnerResolver, SimulationRunnerResolver>();

        using var provider = services.BuildServiceProvider();

        var resolver = provider.GetRequiredService<ISimulationRunnerResolver>();
        var runner = resolver.Resolve("mock");

        Assert.IsType<VirtualLabMockRunner>(runner);
    }

    [Fact]
    public async Task EducationalRunner_GeneratesLedOnOffEvents_ForBlinkProgram()
    {
        var (runner, broadcaster, store, _, _) = CreateStreamingRunner();

        var startedAt = DateTime.UtcNow;
        var startResult = await runner.RunAsync(new SimulationRunContext
        {
            ProjectId = Guid.NewGuid().ToString("N"),
            Mode = "educational",
            MaxDurationMs = 2500,
            MaxInstructionCount = 100,
            DiagramJson = """
            {
              "version": 1,
              "parts": [
                { "type": "board-esp32-devkit-c-v4", "id": "esp" },
                { "type": "wokwi-resistor", "id": "r1" },
                { "type": "wokwi-led", "id": "led1" }
              ],
              "connections": [
                [ "esp:GPIO2", "r1:1" ],
                [ "r1:2", "led1:A" ],
                [ "led1:C", "esp:GND.1" ]
              ]
            }
            """,
            SourceCode = """
            const int LED_PIN = 2;

            void setup() {
              pinMode(LED_PIN, OUTPUT);
            }

            void loop() {
              digitalWrite(LED_PIN, HIGH);
              delay(1000);
              digitalWrite(LED_PIN, LOW);
              delay(1000);
            }
            """
        }, CancellationToken.None);

        // Model streaming: RunAsync() PHẢI trả về gần như ngay lập tức (kick
        // off nền), không còn chờ tính hết như trước.
        Assert.True(
            (DateTime.UtcNow - startedAt) < TimeSpan.FromSeconds(1),
            "RunAsync() phải trả về nhanh (kick off nền), không chờ tính hết.");
        Assert.True(startResult.Success, string.Join("; ", startResult.Errors));
        Assert.Empty(startResult.Events);

        await WaitForCompletionAsync(broadcaster);

        var ledEvents = store.AppendedEvents.Where(IsLedStateEvent).ToList();
        Assert.Contains(ledEvents, item => item.Time == 0 && PayloadString(item, "state") == "on");
        Assert.Contains(ledEvents, item => item.Time == 1000 && PayloadString(item, "state") == "off");
        Assert.Equal(VirtualLabProjectStatuses.Running, store.FinalStatus);
    }

    // Bước 1 (yêu cầu gốc): for-loop trần, không có statement nào theo sau.
    [Fact]
    public async Task EducationalRunner_ForLoopBare_ProducesSixAlternatingEvents()
    {
        var (runner, broadcaster, store, _, _) = CreateStreamingRunner();

        var startResult = await runner.RunAsync(new SimulationRunContext
        {
            ProjectId = Guid.NewGuid().ToString("N"),
            Mode = "educational",
            MaxDurationMs = 6000,
            MaxInstructionCount = 1000,
            DiagramJson = DiagramJsonLedOnPin13,
            SourceCode = """
            void setup() {
              pinMode(13, OUTPUT);
            }

            void loop() {
              for (int i = 0; i < 3; i++) {
                digitalWrite(13, HIGH);
                delay(1000);
                digitalWrite(13, LOW);
                delay(1000);
              }
            }
            """
        }, CancellationToken.None);

        Assert.True(startResult.Success, string.Join("; ", startResult.Errors));
        await WaitForCompletionAsync(broadcaster);

        var ledEvents = store.AppendedEvents.Where(IsLedStateEvent).ToList();
        var expected = new (long Time, string State)[]
        {
            (0, "on"), (1000, "off"), (2000, "on"),
            (3000, "off"), (4000, "on"), (5000, "off")
        };

        foreach (var (time, state) in expected)
        {
            Assert.Contains(ledEvents, item => item.Time == time && PayloadString(item, "state") == state);
        }
    }

    // Bước 1 (biến thể cô lập đúng bug): for-loop CÓ statement theo sau, giống
    // hệt cấu trúc sketch thật (30 vòng + digitalWrite(LOW) + while(true)) đã
    // quan sát lệch nhịp qua UI. Test "trần" ở trên trùng hợp không phân biệt
    // được đúng/sai vì lặp [HIGH,LOW] mãi mãi giống hệt nhau dù for-loop có
    // được hiểu đúng hay bị làm phẳng — statement theo sau mới làm lộ rõ chu
    // kỳ bị lệch. VẪN failing sau Bước 3 — for/while parsing là 1 việc riêng,
    // chưa được yêu cầu sửa (xem trao đổi phạm vi còn bỏ ngỏ).
    [Fact]
    public async Task EducationalRunner_ForLoopWithTrailingStatement_ProducesCorrectSequence()
    {
        var (runner, broadcaster, store, _, _) = CreateStreamingRunner();

        var startResult = await runner.RunAsync(new SimulationRunContext
        {
            ProjectId = Guid.NewGuid().ToString("N"),
            Mode = "educational",
            MaxDurationMs = 9000,
            MaxInstructionCount = 1000,
            DiagramJson = DiagramJsonLedOnPin13,
            SourceCode = """
            void setup() {
              pinMode(13, OUTPUT);
            }

            void loop() {
              for (int i = 0; i < 3; i++) {
                digitalWrite(13, HIGH);
                delay(1000);
                digitalWrite(13, LOW);
                delay(1000);
              }
              digitalWrite(13, LOW);
              delay(500);
            }
            """
        }, CancellationToken.None);

        Assert.True(startResult.Success, string.Join("; ", startResult.Errors));
        await WaitForCompletionAsync(broadcaster);

        var ledEvents = store.AppendedEvents.Where(IsLedStateEvent).ToList();

        // 3 vòng lặp for đúng nghĩa: on/off luân phiên trong đúng 6000ms đầu,
        // rồi 1 lần "off" thừa (statement sau for) tại t=6000, rồi loop() gọi
        // lại từ đầu -> "on" lại tại t=6500 (không phải t=7000/8000...).
        var expected = new (long Time, string State)[]
        {
            (0, "on"), (1000, "off"), (2000, "on"),
            (3000, "off"), (4000, "on"), (5000, "off"),
            (6000, "off"), (6500, "on")
        };

        foreach (var (time, state) in expected)
        {
            Assert.Contains(ledEvents, item => item.Time == time && PayloadString(item, "state") == state);
        }
    }

    // Nền tảng cho Bước 4 (/stop): xác nhận cơ chế hủy qua registry hoạt
    // động đúng ở tầng runner — while(true) thật sự bị chặn lại, không có
    // event nào tới SAU thời điểm TryCancel, và Status cuối cùng là
    // "stopped" chứ không phải "running"/"error".
    [Fact]
    public async Task EducationalRunner_TryCancelMidRun_StopsCleanly_NoMoreEventsAfterCancel()
    {
        var (runner, broadcaster, store, registry, _) = CreateStreamingRunner();
        var projectId = Guid.NewGuid().ToString("N");

        var startResult = await runner.RunAsync(new SimulationRunContext
        {
            ProjectId = projectId,
            Mode = "educational",
            MaxDurationMs = 60_000,
            MaxInstructionCount = 100_000,
            DiagramJson = DiagramJsonLedOnPin13,
            SourceCode = """
            void setup() {
              pinMode(13, OUTPUT);
            }

            void loop() {
              digitalWrite(13, HIGH);
              delay(1000);
              digitalWrite(13, LOW);
              delay(1000);
            }
            """
        }, CancellationToken.None);

        Assert.True(startResult.Success, string.Join("; ", startResult.Errors));

        // Để chạy được ~2 nhịp thật rồi mới hủy giữa chừng.
        await Task.Delay(2500);
        var eventCountBeforeCancel = store.AppendedEvents.Count;

        var cancelled = registry.TryCancel(projectId);
        Assert.True(cancelled, "TryCancel phải tìm thấy lần chạy đang đăng ký.");

        await WaitForCompletionAsync(broadcaster);

        // Không có event nào lọt qua SAU khi đã hủy — cho thời gian dư ra để
        // chắc chắn không có gì "trễ" xuất hiện thêm.
        await Task.Delay(1500);
        Assert.Equal(eventCountBeforeCancel, store.AppendedEvents.Count);

        Assert.Equal(VirtualLabProjectStatuses.Stopped, store.FinalStatus);
        Assert.Equal(VirtualLabProjectStatuses.Stopped, broadcaster.FinalStatus);
    }

    private static bool IsLedStateEvent(SimulationEventResponse item)
    {
        return item.Type == "part-state" && PayloadString(item, "component") == "led";
    }

    private static string? PayloadString(SimulationEventResponse item, string key)
    {
        return item.Payload.TryGetValue(key, out var value) ? value?.ToString() : null;
    }

    private static readonly string DiagramJsonLedOnPin13 = """
    {
      "version": 1,
      "parts": [
        { "type": "board-esp32-devkit-c-v4", "id": "esp" },
        { "type": "wokwi-resistor", "id": "r1" },
        { "type": "wokwi-led", "id": "led1" }
      ],
      "connections": [
        [ "esp:GPIO13", "r1:1" ],
        [ "r1:2", "led1:A" ],
        [ "led1:C", "esp:GND.1" ]
      ]
    }
    """;

    // internal (not private) — reused by RealtimeSimulationInputTests so both
    // test classes build an EducationalSimulationRunner the exact same way as
    // production DI, instead of duplicating the wiring.
    internal static (
        EducationalSimulationRunner Runner,
        FakeSimulationEventBroadcaster Broadcaster,
        FakeSimulationEventStore Store,
        IRunningSimulationRegistry Registry,
        ISimulationInputChannel InputChannel) CreateStreamingRunner(
            ISimulationInputChannel? sharedInputChannel = null)
    {
        var broadcaster = new FakeSimulationEventBroadcaster();
        var store = new FakeSimulationEventStore();
        var registry = new RunningSimulationRegistry();
        // Two runners sharing one channel instance is the real production
        // topology (ISimulationInputChannel is a singleton) — tests that need
        // to prove cross-session isolation on ONE channel pass their own
        // shared instance in; everything else gets an independent one.
        var inputChannel = sharedInputChannel ?? new SimulationInputChannel();

        // IServiceScopeFactory THẬT (không tự chế) — EducationalSimulationRunner
        // tạo scope mới cho mỗi lần chạy nền, đúng như production; store dùng
        // chung 1 instance (đăng ký qua factory closure) để test đọc lại được.
        var services = new ServiceCollection();
        services.AddScoped<ISimulationEventStore>(_ => store);
        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>();

        var configuration = new ConfigurationManager();
        configuration["SimulationRunner:MaxConcurrentRuns"] = "10";

        var runner = new EducationalSimulationRunner(
            new VirtualLabDiagramService(),
            new EducationalProgramAnalyzer(),
            new EducationalEventGenerator(),
            scopeFactory,
            broadcaster,
            registry,
            inputChannel,
            configuration);

        return (runner, broadcaster, store, registry, inputChannel);
    }

    private static async Task WaitForCompletionAsync(FakeSimulationEventBroadcaster broadcaster)
    {
        var completed = await Task.WhenAny(broadcaster.Completed.Task, Task.Delay(TimeSpan.FromSeconds(20)));
        Assert.True(completed == broadcaster.Completed.Task, "Background simulation did not signal completion in time.");
    }

    private sealed class NoOpSimulationEventBroadcaster : ISimulationEventBroadcaster
    {
        public Task BroadcastEventAsync(string projectId, SimulationEventResponse evt, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task BroadcastRunCompletedAsync(string projectId, string status, string? reason, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task BroadcastCompileStartedAsync(string projectId, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task BroadcastCompileFinishedAsync(string projectId, bool success, string? errorSummary, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task BroadcastRunBootingAsync(string projectId, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    public sealed class FakeSimulationEventBroadcaster : ISimulationEventBroadcaster
    {
        public List<SimulationEventResponse> BroadcastedEvents { get; } = new();
        public TaskCompletionSource Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public string? FinalStatus { get; private set; }
        public string? FinalReason { get; private set; }

        public Task BroadcastEventAsync(string projectId, SimulationEventResponse evt, CancellationToken cancellationToken)
        {
            BroadcastedEvents.Add(evt);
            return Task.CompletedTask;
        }

        public Task BroadcastRunCompletedAsync(string projectId, string status, string? reason, CancellationToken cancellationToken)
        {
            FinalStatus = status;
            FinalReason = reason;
            Completed.TrySetResult();
            return Task.CompletedTask;
        }

        public Task BroadcastCompileStartedAsync(string projectId, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task BroadcastCompileFinishedAsync(string projectId, bool success, string? errorSummary, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task BroadcastRunBootingAsync(string projectId, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    public sealed class FakeSimulationEventStore : ISimulationEventStore
    {
        public List<SimulationEventResponse> AppendedEvents { get; } = new();
        public string? FinalStatus { get; private set; }

        public Task AppendEventAsync(string projectId, SimulationEventResponse evt, CancellationToken cancellationToken)
        {
            AppendedEvents.Add(evt);
            return Task.CompletedTask;
        }

        public Task MarkRunFinishedAsync(string projectId, string status, CancellationToken cancellationToken)
        {
            FinalStatus = status;
            return Task.CompletedTask;
        }
    }
}
