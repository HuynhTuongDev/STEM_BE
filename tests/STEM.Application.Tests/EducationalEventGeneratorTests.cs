using System.Diagnostics;
using STEM.Application.Dtos.Simulation;
using STEM.Application.UseCases.Simulation;
using STEM.Application.UseCases.Simulation.Abstractions;
using STEM.Application.UseCases.Simulation.Runners.Educational;

namespace STEM.Application.Tests;

public sealed class EducationalEventGeneratorTests
{
    // Bước 2 (yêu cầu gốc): đo thời gian THẬT trôi qua giữa 2 lần gọi
    // callback cách nhau bởi delay(1000) trong sketch — xác nhận Executor
    // dùng Task.Delay thật, không phải chỉ cộng dồn "thời gian ảo".
    [Fact]
    public async Task GenerateAsync_WaitsRealWallClockTime_BetweenDelayedEvents()
    {
        var generator = new EducationalEventGenerator();
        var analyzer = new EducationalProgramAnalyzer();
        var program = analyzer.Analyze("""
            void setup() { pinMode(13, OUTPUT); }
            void loop() {
              digitalWrite(13, HIGH);
              delay(1000);
              digitalWrite(13, LOW);
              delay(1000);
            }
            """);

        var snapshot = new VirtualLabRuntimeDiagramSnapshot(Array.Empty<VirtualLabRuntimeComponent>());
        var context = new SimulationRunContext
        {
            ProjectId = Guid.NewGuid().ToString("N"),
            Mode = "educational",
            SourceCode = "unused-in-this-test",
            DiagramJson = "{}",
            MaxDurationMs = 2500,
            MaxInstructionCount = 100
        };

        var callbackTimestamps = new List<(SimulationEventResponse Event, long ElapsedMs)>();
        var stopwatch = Stopwatch.StartNew();

        Task OnEventEmitted(SimulationEventResponse evt)
        {
            callbackTimestamps.Add((evt, stopwatch.ElapsedMilliseconds));
            return Task.CompletedTask;
        }

        var result = await generator.GenerateAsync(program, snapshot, context, OnEventEmitted, CancellationToken.None);

        Assert.True(result.Success, string.Join("; ", result.Errors));

        var digitalWrites = callbackTimestamps
            .Where(item => item.Event.Type == "pin-state" &&
                           item.Event.Payload.TryGetValue("operation", out var op) &&
                           op?.ToString() == "digitalWrite")
            .ToList();

        Assert.True(digitalWrites.Count >= 2, $"Expected at least 2 digitalWrite events, got {digitalWrites.Count}");

        // digitalWrite(HIGH) đầu tiên phải tới gần như ngay lập tức (t=0
        // logic, không có delay trước nó).
        Assert.InRange(digitalWrites[0].ElapsedMs, 0, 300);

        // digitalWrite(LOW) thứ 2 phải tới SAU khi đã chờ ~1000ms thật —
        // không phải tức thời như trước khi sửa (AdvanceTime cũ chỉ cộng
        // dồn state.Time, callback sẽ bắn ra gần như đồng thời với cái đầu).
        var elapsedBetween = digitalWrites[1].ElapsedMs - digitalWrites[0].ElapsedMs;
        Assert.InRange(elapsedBetween, 800, 1400);
    }

    // Bước 3.3 (yêu cầu gốc, viết trước để có sẵn khi triển khai Bước 3):
    // while(true) với MaxInstructionCount nhỏ phải dừng đúng giới hạn, không
    // treo, không lệch nhịp.
    [Fact]
    public async Task GenerateAsync_WhileTrue_StopsAtMaxInstructionCount_NoHang()
    {
        var generator = new EducationalEventGenerator();
        var analyzer = new EducationalProgramAnalyzer();
        var program = analyzer.Analyze("""
            void setup() { pinMode(13, OUTPUT); }
            void loop() {
              while (true) {
                digitalWrite(13, HIGH);
                delay(10);
                digitalWrite(13, LOW);
                delay(10);
              }
            }
            """);

        var snapshot = new VirtualLabRuntimeDiagramSnapshot(Array.Empty<VirtualLabRuntimeComponent>());
        var context = new SimulationRunContext
        {
            ProjectId = Guid.NewGuid().ToString("N"),
            Mode = "educational",
            SourceCode = "unused-in-this-test",
            DiagramJson = "{}",
            MaxDurationMs = 60_000,
            MaxInstructionCount = 50
        };

        var events = new List<SimulationEventResponse>();
        Task OnEventEmitted(SimulationEventResponse evt)
        {
            events.Add(evt);
            return Task.CompletedTask;
        }

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var result = await generator.GenerateAsync(program, snapshot, context, OnEventEmitted, cts.Token);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("MaxInstructionCount", StringComparison.OrdinalIgnoreCase));
        Assert.True(events.Count > 0);
    }

    // Yêu cầu điều tra: for(i<3){HIGH,delay,LOW,delay} rồi while(true){delay(1000)}
    // ở CUỐI loop() — đúng ngữ nghĩa C++/Arduino thật, while(true) không bao giờ
    // return, loop() không bao giờ được gọi lại sau đó -> LED phải nháy đúng 3
    // lần rồi TẮT HẲN VĨNH VIỄN, không nháy tiếp. Chạy đủ 10 giây thật, KHÔNG hủy
    // CancellationToken giữa chừng — mô phỏng đúng "học sinh không bấm Stop".
    [Fact]
    public async Task GenerateAsync_ForLoopThenWhileTrueAtEnd_BlinksExactly3Times_ThenStaysOffForever()
    {
        var generator = new EducationalEventGenerator();
        var analyzer = new EducationalProgramAnalyzer();
        var program = analyzer.Analyze("""
            const int LED_PIN = 13;

            void setup() {
              pinMode(LED_PIN, OUTPUT);
            }

            void loop() {
              for (int i = 0; i < 3; i++) {
                digitalWrite(LED_PIN, HIGH);
                delay(1000);
                digitalWrite(LED_PIN, LOW);
                delay(1000);
              }
              while (true) {
                delay(1000);
              }
            }
            """);

        var snapshot = new VirtualLabRuntimeDiagramSnapshot(Array.Empty<VirtualLabRuntimeComponent>());
        var context = new SimulationRunContext
        {
            ProjectId = Guid.NewGuid().ToString("N"),
            Mode = "educational",
            SourceCode = "unused-in-this-test",
            DiagramJson = "{}",
            MaxDurationMs = 10_500,
            MaxInstructionCount = 10_000
        };

        var digitalWriteEvents = new List<(SimulationEventResponse Event, long ElapsedMs)>();
        var stopwatch = Stopwatch.StartNew();

        Task OnEventEmitted(SimulationEventResponse evt)
        {
            if (evt.Type == "pin-state" &&
                evt.Payload.TryGetValue("operation", out var op) &&
                op?.ToString() == "digitalWrite")
            {
                digitalWriteEvents.Add((evt, stopwatch.ElapsedMilliseconds));
            }

            return Task.CompletedTask;
        }

        // KHÔNG hủy CancellationToken — chạy đủ 10s thật, đúng như học sinh
        // không bấm Stop.
        var result = await generator.GenerateAsync(program, snapshot, context, OnEventEmitted, CancellationToken.None);

        Assert.True(result.Success, string.Join("; ", result.Errors));

        // Ba vòng tạo đúng sáu lần ghi tại t=0..5000. Delay cuối đưa timeline
        // tới t=6000 rồi while(true) chỉ delay, nên không còn GPIO event nào.
        var actualSequence = digitalWriteEvents
            .Select(item => (item.Event.Time, Value: item.Event.Payload["value"]?.ToString()))
            .ToArray();
        var expectedSequence = new (long Time, string? Value)[]
        {
            (0, "HIGH"), (1000, "LOW"),
            (2000, "HIGH"), (3000, "LOW"),
            (4000, "HIGH"), (5000, "LOW")
        };

        Assert.Equal(expectedSequence, actualSequence);
    }
}
