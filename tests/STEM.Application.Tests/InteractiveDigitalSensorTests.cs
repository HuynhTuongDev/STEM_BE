using STEM.Application.Dtos.Simulation;
using STEM.Application.UseCases.Simulation;
using STEM.Application.UseCases.Simulation.Abstractions;
using STEM.Application.UseCases.Simulation.Runtime;
using STEM.Core.Entities.Simulations;

namespace STEM.Application.Tests;

// INTERACTIVE SENSOR CONTROLS milestone. Same pattern as
// RealtimeSimulationInputTests.cs's Button coverage — real
// EducationalSimulationRunner, real if/else program, real
// SetSimulationInput-shaped writes into the SAME ISimulationInputChannel, no
// second interaction system. Proves DigitalSensorModel.TryReadLiveInput
// (built last milestone, unused by any FE control until this one) actually
// works end-to-end for PIR/Water Leak/Vibration, not just in isolation.
public sealed class InteractiveDigitalSensorTests
{
    private static string DiagramJson(string sensorType, string sensorId, string sensorPin, string gpio) => $$"""
    {
      "version": 1,
      "parts": [
        { "type": "board-esp32-devkit-c-v4", "id": "esp" },
        { "type": "{{sensorType}}", "id": "{{sensorId}}" },
        { "type": "wokwi-led", "id": "led1" }
      ],
      "connections": [
        [ "{{sensorId}}:{{sensorPin}}", "esp:{{gpio}}" ],
        [ "{{sensorId}}:VCC", "esp:3V3" ],
        [ "{{sensorId}}:GND", "esp:GND.1" ],
        [ "esp:GPIO13", "led1:A" ],
        [ "led1:C", "esp:GND.2" ]
      ]
    }
    """;

    private static string ReactiveProgram(int sensorPinNumber) => $$"""
    const int SENSOR_PIN = {{sensorPinNumber}};
    const int LED_PIN = 13;

    void setup() {
      pinMode(SENSOR_PIN, INPUT);
      pinMode(LED_PIN, OUTPUT);
    }

    void loop() {
      if (digitalRead(SENSOR_PIN) == HIGH) {
        digitalWrite(LED_PIN, HIGH);
      } else {
        digitalWrite(LED_PIN, LOW);
      }
      delay(200);
    }
    """;

    public static IEnumerable<object[]> SensorCases()
    {
        yield return new object[] { "wokwi-pir-motion-sensor", "pir1", "OUT", "GPIO14", 14 };
        yield return new object[] { "wokwi-water-leak-sensor", "wl1", "S", "GPIO27", 27 };
        yield return new object[] { "wokwi-vibration-sensor", "vib1", "OUT", "GPIO25", 25 };
        // Optional 4th sensor (STEP 22) — same generic control, same test.
        yield return new object[] { "wokwi-rain-sensor", "rain1", "DO", "GPIO26", 26 };
    }

    [Theory]
    [MemberData(nameof(SensorCases))]
    public async Task Toggle_ReactsLive_WithoutRestart_ThenOffTurnsLedOffAgain(
        string sensorType, string sensorId, string sensorPin, string gpio, int gpioNumber)
    {
        var (runner, broadcaster, store, _, inputChannel) = SimulationRunnerResolverTests.CreateStreamingRunner();
        var projectId = Guid.NewGuid().ToString("N");

        var startResult = await runner.RunAsync(new SimulationRunContext
        {
            ProjectId = projectId,
            Mode = "educational",
            MaxDurationMs = 3000,
            MaxInstructionCount = 1000,
            DiagramJson = DiagramJson(sensorType, sensorId, sensorPin, gpio),
            SourceCode = ReactiveProgram(gpioNumber)
        }, CancellationToken.None);

        Assert.True(startResult.Success, string.Join("; ", startResult.Errors));

        // Baseline (no live input, no scenario): LED off.
        await Task.Delay(450);
        var eventsBefore = store.AppendedEvents.Where(IsLedStateEvent).ToList();
        Assert.NotEmpty(eventsBefore);
        Assert.All(eventsBefore, item => Assert.Equal("off", PayloadString(item, "state")));

        // "Toggle on" — same call shape virtualLabHub.setSimulationInput makes.
        var onAccepted = inputChannel.TrySetInput(new SimulationInputEvent(
            projectId, sensorId, gpio, SimulationInputType.Digital, true));
        Assert.True(onAccepted, "Session must be registered while RunAsync's background task is still running.");

        await Task.Delay(650);
        var eventsWhileOn = store.AppendedEvents.Where(IsLedStateEvent).ToList();
        Assert.Contains(eventsWhileOn, item => PayloadString(item, "state") == "on");

        // "Toggle off" — same running session, no new RunAsync call.
        var offAccepted = inputChannel.TrySetInput(new SimulationInputEvent(
            projectId, sensorId, gpio, SimulationInputType.Digital, false));
        Assert.True(offAccepted);

        await Task.Delay(650);
        var eventsAfterOff = store.AppendedEvents.Where(IsLedStateEvent).ToList();
        Assert.Equal("off", PayloadString(eventsAfterOff[^1], "state"));

        await WaitForCompletionAsync(broadcaster);
        Assert.Equal(VirtualLabProjectStatuses.Running, store.FinalStatus);
    }

    [Fact]
    public void MultipleSessions_PirStateIsolated_ToggleInOneDoesNotLeakToAnother()
    {
        var channel = new SimulationInputChannel();
        var inputsA = new System.Collections.Concurrent.ConcurrentDictionary<string, object>();
        var inputsB = new System.Collections.Concurrent.ConcurrentDictionary<string, object>();
        channel.RegisterSession("session-a", inputsA);
        channel.RegisterSession("session-b", inputsB);

        channel.TrySetInput(new SimulationInputEvent("session-a", "pir1", "OUT", SimulationInputType.Digital, true));

        Assert.True((bool)inputsA["pir1"]);
        Assert.False(inputsB.ContainsKey("pir1"));
    }

    [Fact]
    public async Task StoppedSession_RejectsSensorToggle_NoCrash()
    {
        var (runner, broadcaster, _, registry, inputChannel) = SimulationRunnerResolverTests.CreateStreamingRunner();
        var projectId = Guid.NewGuid().ToString("N");

        await runner.RunAsync(new SimulationRunContext
        {
            ProjectId = projectId,
            Mode = "educational",
            MaxDurationMs = 60_000,
            MaxInstructionCount = 100_000,
            DiagramJson = DiagramJson("wokwi-pir-motion-sensor", "pir1", "OUT", "GPIO14"),
            SourceCode = ReactiveProgram(14)
        }, CancellationToken.None);

        await Task.Delay(300);
        Assert.True(inputChannel.TrySetInput(new SimulationInputEvent(
            projectId, "pir1", "GPIO14", SimulationInputType.Digital, true)));

        registry.TryCancel(projectId);
        await WaitForCompletionAsync(broadcaster);
        await Task.Delay(200);

        var acceptedAfterStop = inputChannel.TrySetInput(new SimulationInputEvent(
            projectId, "pir1", "GPIO14", SimulationInputType.Digital, false));
        Assert.False(acceptedAfterStop, "A stopped simulation must not accept sensor input anymore, and must not throw.");
    }

    [Fact]
    public async Task InvalidComponentId_TrySetInput_ReturnsFalse_DoesNotAffectRunningSession()
    {
        var (runner, broadcaster, store, _, inputChannel) = SimulationRunnerResolverTests.CreateStreamingRunner();
        var projectId = Guid.NewGuid().ToString("N");

        var startResult = await runner.RunAsync(new SimulationRunContext
        {
            ProjectId = projectId,
            Mode = "educational",
            MaxDurationMs = 1500,
            MaxInstructionCount = 1000,
            DiagramJson = DiagramJson("wokwi-pir-motion-sensor", "pir1", "OUT", "GPIO14"),
            SourceCode = ReactiveProgram(14)
        }, CancellationToken.None);
        Assert.True(startResult.Success);

        // Wrong project id — must not throw, must simply be rejected.
        var accepted = inputChannel.TrySetInput(new SimulationInputEvent(
            "does-not-exist", "pir1", "GPIO14", SimulationInputType.Digital, true));
        Assert.False(accepted);

        await WaitForCompletionAsync(broadcaster);
        var ledEvents = store.AppendedEvents.Where(IsLedStateEvent).ToList();
        Assert.All(ledEvents, item => Assert.Equal("off", PayloadString(item, "state")));
    }

    private static bool IsLedStateEvent(SimulationEventResponse item) =>
        item.Type == "part-state" && PayloadString(item, "component") == "led";

    private static string? PayloadString(SimulationEventResponse item, string key) =>
        item.Payload.TryGetValue(key, out var value) ? value?.ToString() : null;

    private static async Task WaitForCompletionAsync(SimulationRunnerResolverTests.FakeSimulationEventBroadcaster broadcaster)
    {
        await Task.WhenAny(broadcaster.Completed.Task, Task.Delay(TimeSpan.FromSeconds(20)));
    }
}
