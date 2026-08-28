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

    private const string DiagramJsonPotentiometerAndLed = """
    {
      "version": 1,
      "parts": [
        { "type": "board-esp32-devkit-c-v4", "id": "esp" },
        { "type": "wokwi-potentiometer", "id": "pot1" },
        { "type": "wokwi-resistor", "id": "r1" },
        { "type": "wokwi-led", "id": "led1" }
      ],
      "connections": [
        [ "pot1:SIG", "esp:GPIO34" ],
        [ "pot1:GND", "esp:GND.1" ],
        [ "pot1:VCC", "esp:3V3" ],
        [ "esp:GPIO13", "r1:1" ],
        [ "r1:2", "led1:A" ],
        [ "led1:C", "esp:GND.2" ]
      ]
    }
    """;

    private const string PotentiometerReactiveProgram = """
    const int POT = 34;
    const int LED = 13;

    void setup() {
      pinMode(LED, OUTPUT);
    }

    void loop() {
      int value = analogRead(POT);

      if (value > 2000) {
        digitalWrite(LED, HIGH);
      } else {
        digitalWrite(LED, LOW);
      }

      delay(50);
    }
    """;

    [Fact]
    public async Task PotentiometerSlider_ReactsLive_CrossingThresholdBothWays_WithoutRestart()
    {
        var (runner, broadcaster, store, _, inputChannel) = SimulationRunnerResolverTests.CreateStreamingRunner();
        var projectId = Guid.NewGuid().ToString("N");

        var startResult = await runner.RunAsync(new SimulationRunContext
        {
            ProjectId = projectId,
            Mode = "educational",
            MaxDurationMs = 3000,
            MaxInstructionCount = 2000,
            DiagramJson = DiagramJsonPotentiometerAndLed,
            SourceCode = PotentiometerReactiveProgram
        }, CancellationToken.None);

        Assert.True(startResult.Success, string.Join("; ", startResult.Errors));

        // Default (nobody moved the slider yet) reads as 0 -> below threshold -> LED off.
        await Task.Delay(250);
        var eventsBeforeMove = store.AppendedEvents.Where(IsLedStateEvent).ToList();
        Assert.NotEmpty(eventsBeforeMove);
        Assert.All(eventsBeforeMove, item => Assert.Equal("off", PayloadString(item, "state")));

        // Slider above threshold (2500 > 2000).
        Assert.True(inputChannel.TrySetInput(new SimulationInputEvent(
            projectId, "pot1", "34", SimulationInputType.Analog, 2500)));

        await Task.Delay(300);
        var eventsAboveThreshold = store.AppendedEvents.Where(IsLedStateEvent).ToList();
        Assert.Contains(eventsAboveThreshold, item => PayloadString(item, "state") == "on");

        // Slider back below threshold (500 < 2000) — same run, no restart.
        Assert.True(inputChannel.TrySetInput(new SimulationInputEvent(
            projectId, "pot1", "34", SimulationInputType.Analog, 500)));

        await Task.Delay(300);
        var eventsAfterLowered = store.AppendedEvents.Where(IsLedStateEvent).ToList();
        Assert.Equal("off", PayloadString(eventsAfterLowered[^1], "state"));

        await WaitForCompletionAsync(broadcaster);
        Assert.Equal(VirtualLabProjectStatuses.Running, store.FinalStatus);
    }

    [Fact]
    public async Task MultiSessionIsolation_HoldsForAnalogInputToo()
    {
        // ONE shared channel instance for both runners — matches the real
        // production topology (ISimulationInputChannel is a singleton serving
        // every concurrently-running session), unlike the other tests in this
        // class which each get their own isolated channel for simplicity.
        var sharedChannel = new SimulationInputChannel();
        var (runnerA, broadcasterA, storeA, registryA, channel) = SimulationRunnerResolverTests.CreateStreamingRunner(sharedChannel);
        var (runnerB, broadcasterB, storeB, registryB, _) = SimulationRunnerResolverTests.CreateStreamingRunner(sharedChannel);
        var projectA = Guid.NewGuid().ToString("N");
        var projectB = Guid.NewGuid().ToString("N");

        try
        {
            await runnerA.RunAsync(new SimulationRunContext
            {
                ProjectId = projectA,
                Mode = "educational",
                MaxDurationMs = 10_000,
                MaxInstructionCount = 100_000,
                DiagramJson = DiagramJsonPotentiometerAndLed,
                SourceCode = PotentiometerReactiveProgram
            }, CancellationToken.None);
            await runnerB.RunAsync(new SimulationRunContext
            {
                ProjectId = projectB,
                Mode = "educational",
                MaxDurationMs = 10_000,
                MaxInstructionCount = 100_000,
                DiagramJson = DiagramJsonPotentiometerAndLed,
                SourceCode = PotentiometerReactiveProgram
            }, CancellationToken.None);

            await Task.Delay(200);

            // Set project A's slider high — must accept for A, and must NEVER
            // be reachable by asking for B's session under A's own id (there's
            // only one channel now, so this genuinely exercises key isolation,
            // not "two independent objects don't share state").
            Assert.True(channel.TrySetInput(new SimulationInputEvent(
                projectA, "pot1", "34", SimulationInputType.Analog, 3000)));

            await Task.Delay(300);
            var bEvents = storeB.AppendedEvents.Where(IsLedStateEvent).ToList();
            Assert.NotEmpty(bEvents);
            Assert.All(bEvents, item => Assert.Equal("off", PayloadString(item, "state")));

            var aEvents = storeA.AppendedEvents.Where(IsLedStateEvent).ToList();
            Assert.Contains(aEvents, item => PayloadString(item, "state") == "on");
        }
        finally
        {
            registryA.TryCancel(projectA);
            registryB.TryCancel(projectB);
            await WaitForCompletionAsync(broadcasterA);
            await WaitForCompletionAsync(broadcasterB);
        }
    }

    private const string DiagramJsonLightSensorAndLed = """
    {
      "version": 1,
      "parts": [
        { "type": "board-esp32-devkit-c-v4", "id": "esp" },
        { "type": "wokwi-photoresistor-sensor", "id": "light1" },
        { "type": "wokwi-resistor", "id": "r1" },
        { "type": "wokwi-led", "id": "led1" }
      ],
      "connections": [
        [ "light1:AO", "esp:GPIO35" ],
        [ "light1:GND", "esp:GND.1" ],
        [ "light1:VCC", "esp:3V3" ],
        [ "esp:GPIO13", "r1:1" ],
        [ "r1:2", "led1:A" ],
        [ "led1:C", "esp:GND.2" ]
      ]
    }
    """;

    // Below a brightness threshold -> too dark -> turn the LED on (a night
    // light, not an alarm) — deliberately the OPPOSITE polarity from the
    // potentiometer test above, to prove this isn't just reusing the same
    // branch by coincidence.
    private const string LightSensorReactiveProgram = """
    const int LIGHT_PIN = 35;
    const int LED_PIN = 13;

    void setup() {
      pinMode(LED_PIN, OUTPUT);
    }

    void loop() {
      int brightness = analogRead(LIGHT_PIN);

      if (brightness < 1000) {
        digitalWrite(LED_PIN, HIGH);
      } else {
        digitalWrite(LED_PIN, LOW);
      }

      delay(50);
    }
    """;

    [Fact]
    public async Task LightSensorValue_ReactsLive_CrossingThresholdBothWays_WithoutRestart()
    {
        var (runner, broadcaster, store, _, inputChannel) = SimulationRunnerResolverTests.CreateStreamingRunner();
        var projectId = Guid.NewGuid().ToString("N");

        var startResult = await runner.RunAsync(new SimulationRunContext
        {
            ProjectId = projectId,
            Mode = "educational",
            MaxDurationMs = 3000,
            MaxInstructionCount = 2000,
            DiagramJson = DiagramJsonLightSensorAndLed,
            SourceCode = LightSensorReactiveProgram
        }, CancellationToken.None);

        Assert.True(startResult.Success, string.Join("; ", startResult.Errors));

        // Default (nobody set a sensor value yet) reads as 0 -> below 1000 -> "dark" -> LED on.
        await Task.Delay(250);
        var eventsBeforeSet = store.AppendedEvents.Where(IsLedStateEvent).ToList();
        Assert.NotEmpty(eventsBeforeSet);
        Assert.All(eventsBeforeSet, item => Assert.Equal("on", PayloadString(item, "state")));

        // Bright room (3000 > 1000 threshold) -> LED off.
        Assert.True(inputChannel.TrySetInput(new SimulationInputEvent(
            projectId, "light1", "35", SimulationInputType.Sensor, 3000, SensorKind: "light")));

        await Task.Delay(300);
        var eventsBright = store.AppendedEvents.Where(IsLedStateEvent).ToList();
        Assert.Contains(eventsBright, item => PayloadString(item, "state") == "off");

        // Dark again (200 < 1000) -> LED back on, same run.
        Assert.True(inputChannel.TrySetInput(new SimulationInputEvent(
            projectId, "light1", "35", SimulationInputType.Sensor, 200, SensorKind: "light")));

        await Task.Delay(300);
        var eventsDarkAgain = store.AppendedEvents.Where(IsLedStateEvent).ToList();
        Assert.Equal("on", PayloadString(eventsDarkAgain[^1], "state"));

        await WaitForCompletionAsync(broadcaster);
        Assert.Equal(VirtualLabProjectStatuses.Running, store.FinalStatus);
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
