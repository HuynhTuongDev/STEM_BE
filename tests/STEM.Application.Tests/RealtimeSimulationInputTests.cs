using System.Collections.Concurrent;
using STEM.Application.Dtos.Simulation;
using STEM.Application.UseCases.Simulation;
using STEM.Application.UseCases.Simulation.Abstractions;
using STEM.Application.UseCases.Simulation.Runtime;
using STEM.Core.Entities.Simulations;

namespace STEM.Application.Tests;

public sealed class RealtimeSimulationInputTests
{
    // === ISimulationInputChannel unit tests (no timing, no runner) ===

    [Fact]
    public void TrySetInput_ReturnsFalse_WhenSessionNotRegistered()
    {
        var channel = new SimulationInputChannel();

        var accepted = channel.TrySetInput(new SimulationInputEvent(
            "unknown-project", "button1", "4", SimulationInputType.Digital, true));

        Assert.False(accepted);
    }

    [Fact]
    public void TrySetInput_ReturnsFalse_ForWrongProjectId()
    {
        var channel = new SimulationInputChannel();
        var inputs = new ConcurrentDictionary<string, object>();
        channel.RegisterSession("project-a", inputs);

        var accepted = channel.TrySetInput(new SimulationInputEvent(
            "project-b", "button1", "4", SimulationInputType.Digital, true));

        Assert.False(accepted);
        Assert.Empty(inputs);
    }

    [Fact]
    public void TrySetInput_WritesValue_WhenSessionRegistered()
    {
        var channel = new SimulationInputChannel();
        var inputs = new ConcurrentDictionary<string, object>();
        channel.RegisterSession("project-a", inputs);

        var accepted = channel.TrySetInput(new SimulationInputEvent(
            "project-a", "button1", "4", SimulationInputType.Digital, true));

        Assert.True(accepted);
        Assert.Equal(true, inputs["button1"]);
    }

    [Fact]
    public void TrySetInput_ReturnsFalse_AfterUnregisterSession()
    {
        var channel = new SimulationInputChannel();
        var inputs = new ConcurrentDictionary<string, object>();
        channel.RegisterSession("project-a", inputs);
        channel.UnregisterSession("project-a");

        var accepted = channel.TrySetInput(new SimulationInputEvent(
            "project-a", "button1", "4", SimulationInputType.Digital, true));

        Assert.False(accepted);
    }

    [Fact]
    public void MultipleSessions_AreIsolated()
    {
        var channel = new SimulationInputChannel();
        var inputsA = new ConcurrentDictionary<string, object>();
        var inputsB = new ConcurrentDictionary<string, object>();
        channel.RegisterSession("project-a", inputsA);
        channel.RegisterSession("project-b", inputsB);

        channel.TrySetInput(new SimulationInputEvent("project-a", "button1", "4", SimulationInputType.Digital, true));

        Assert.True((bool)inputsA["button1"]);
        Assert.False(inputsB.ContainsKey("button1"));
    }

    // === End-to-end vertical slice: real EducationalSimulationRunner, real
    // if/else program, real Task.Delay-paced loop, real SetSimulationInput-style
    // write via the channel — proves Button press/release -> firmware/model
    // logic -> LED output, with NO restart and NO recompile (single RunAsync
    // call for the whole test). ===

    private const string DiagramJsonButtonAndLed = """
    {
      "version": 1,
      "parts": [
        { "type": "board-esp32-devkit-c-v4", "id": "esp" },
        { "type": "wokwi-pushbutton", "id": "button1" },
        { "type": "wokwi-resistor", "id": "r1" },
        { "type": "wokwi-led", "id": "led1" }
      ],
      "connections": [
        [ "button1:1.l", "esp:GPIO4" ],
        [ "button1:2.r", "esp:GND.1" ],
        [ "esp:GPIO13", "r1:1" ],
        [ "r1:2", "led1:A" ],
        [ "led1:C", "esp:GND.1" ]
      ]
    }
    """;

    private const string ButtonReactiveProgram = """
    const int BUTTON_PIN = 4;
    const int LED_PIN = 13;

    void setup() {
      pinMode(BUTTON_PIN, INPUT);
      pinMode(LED_PIN, OUTPUT);
    }

    void loop() {
      if (digitalRead(BUTTON_PIN) == HIGH) {
        digitalWrite(LED_PIN, HIGH);
      } else {
        digitalWrite(LED_PIN, LOW);
      }
      delay(200);
    }
    """;

    [Fact]
    public async Task ButtonPress_ReactsLive_WithoutRestart_ThenReleaseTurnsLedOffAgain()
    {
        var (runner, broadcaster, store, _, inputChannel) = SimulationRunnerResolverTests.CreateStreamingRunner();
        var projectId = Guid.NewGuid().ToString("N");

        var startResult = await runner.RunAsync(new SimulationRunContext
        {
            ProjectId = projectId,
            Mode = "educational",
            MaxDurationMs = 3000,
            MaxInstructionCount = 1000,
            DiagramJson = DiagramJsonButtonAndLed,
            SourceCode = ButtonReactiveProgram
        }, CancellationToken.None);

        Assert.True(startResult.Success, string.Join("; ", startResult.Errors));

        // Before any press: give it ~2 iterations (400ms) to prove the baseline
        // (unpressed) behavior is "off", exactly like before this feature existed.
        await Task.Delay(450);
        var eventsBeforePress = store.AppendedEvents.Where(IsLedStateEvent).ToList();
        Assert.NotEmpty(eventsBeforePress);
        Assert.All(eventsBeforePress, item => Assert.Equal("off", PayloadString(item, "state")));

        // Press — same call shape SetSimulationInput makes into the channel.
        var pressAccepted = inputChannel.TrySetInput(new SimulationInputEvent(
            projectId, "button1", "4", SimulationInputType.Digital, true));
        Assert.True(pressAccepted, "Session must be registered while RunAsync's background task is still running.");

        // Let a few more iterations run WHILE pressed — no new Run request, no
        // recompile, same background task from the single RunAsync call above.
        await Task.Delay(650);
        var eventsWhilePressed = store.AppendedEvents.Where(IsLedStateEvent).ToList();
        Assert.Contains(eventsWhilePressed, item => PayloadString(item, "state") == "on");

        // Release.
        var releaseAccepted = inputChannel.TrySetInput(new SimulationInputEvent(
            projectId, "button1", "4", SimulationInputType.Digital, false));
        Assert.True(releaseAccepted);

        await Task.Delay(650);
        var eventsAfterRelease = store.AppendedEvents.Where(IsLedStateEvent).ToList();
        // Last LED state observed must be "off" again after release, proving
        // the SAME running loop picked up the new value (not a one-shot latch).
        Assert.Equal("off", PayloadString(eventsAfterRelease[^1], "state"));

        await WaitForCompletionAsync(broadcaster);
        Assert.Equal(VirtualLabProjectStatuses.Running, store.FinalStatus);
    }

    [Fact]
    public async Task InputChannel_IsUnregistered_AfterSimulationStops()
    {
        var (runner, broadcaster, _, registry, inputChannel) = SimulationRunnerResolverTests.CreateStreamingRunner();
        var projectId = Guid.NewGuid().ToString("N");

        await runner.RunAsync(new SimulationRunContext
        {
            ProjectId = projectId,
            Mode = "educational",
            MaxDurationMs = 60_000,
            MaxInstructionCount = 100_000,
            DiagramJson = DiagramJsonButtonAndLed,
            SourceCode = ButtonReactiveProgram
        }, CancellationToken.None);

        await Task.Delay(300);
        Assert.True(inputChannel.TrySetInput(new SimulationInputEvent(
            projectId, "button1", "4", SimulationInputType.Digital, true)));

        registry.TryCancel(projectId);
        await WaitForCompletionAsync(broadcaster);
        await Task.Delay(200);

        var acceptedAfterStop = inputChannel.TrySetInput(new SimulationInputEvent(
            projectId, "button1", "4", SimulationInputType.Digital, false));
        Assert.False(acceptedAfterStop, "A stopped simulation must not accept input anymore.");
    }

    private static bool IsLedStateEvent(SimulationEventResponse item)
    {
        return item.Type == "part-state" && PayloadString(item, "component") == "led";
    }

    private static string? PayloadString(SimulationEventResponse item, string key)
    {
        return item.Payload.TryGetValue(key, out var value) ? value?.ToString() : null;
    }

    private static async Task WaitForCompletionAsync(SimulationRunnerResolverTests.FakeSimulationEventBroadcaster broadcaster)
    {
        await Task.WhenAny(broadcaster.Completed.Task, Task.Delay(TimeSpan.FromSeconds(20)));
    }
}
