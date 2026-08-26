using STEM.Application.Dtos.Simulation;
using STEM.Application.UseCases.Simulation;
using STEM.Application.UseCases.Simulation.Abstractions;
using STEM.Application.UseCases.Simulation.Runners.Educational;

namespace STEM.Application.Tests;

// EDUCATIONAL SYNTAX COMPATIBILITY HARDENING (2026-08-26): regression coverage
// for the exact syntax gap a live browser test found in the shipped Flame
// Sensor sample — "bool detected = (digitalRead(PIN) == HIGH); if (detected)
// ...; digitalWrite(PIN, detected ? HIGH : LOW);" compiled fine but produced
// ZERO loop() instructions (silently, no warning) because none of the three
// shapes existed anywhere in EducationalProgramAnalyzer's regex set. These
// tests pin the fix: a `bool` local assigned from a `digitalRead(pin)`
// comparison is now a resolvable alias for `if (name)`/`if (!name)` and for
// `digitalWrite(pin, name ? A : B)`, and a bare-identifier Serial.print/
// println argument resolves to the LIVE value instead of a static
// "<name>" placeholder.
//
// No button/digital-sensor component is wired into any of these diagrams on
// purpose: with an empty component snapshot, digitalRead's fallback is
// deterministic — INPUT_PULLUP always reads HIGH, plain INPUT always reads
// LOW (see EducationalEventGenerator's ReadDigitalConditionValue/If cases) —
// which is exactly what lets these tests assert a specific branch was taken
// without needing to fake live ISimulationInputChannel input.
public sealed class EducationalSyntaxCompatibilityTests
{
    private static async Task<List<SimulationEventResponse>> RunAsync(string sketch, int maxDurationMs = 60)
    {
        var generator = new EducationalEventGenerator();
        var analyzer = new EducationalProgramAnalyzer();
        var program = analyzer.Analyze(sketch);

        var snapshot = new VirtualLabRuntimeDiagramSnapshot(Array.Empty<VirtualLabRuntimeComponent>());
        var context = new SimulationRunContext
        {
            ProjectId = Guid.NewGuid().ToString("N"),
            Mode = "educational",
            SourceCode = "unused-in-this-test",
            DiagramJson = "{}",
            MaxDurationMs = maxDurationMs,
            MaxInstructionCount = 1000
        };

        var events = new List<SimulationEventResponse>();
        Task OnEventEmitted(SimulationEventResponse evt)
        {
            events.Add(evt);
            return Task.CompletedTask;
        }

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var result = await generator.GenerateAsync(program, snapshot, context, OnEventEmitted, cts.Token);
        Assert.True(result.Success, string.Join("; ", result.Errors));
        return events;
    }

    private static IEnumerable<(string Pin, string Value)> DigitalWrites(IEnumerable<SimulationEventResponse> events) =>
        events
            .Where(e => e.Type == "pin-state" &&
                        e.Payload.TryGetValue("operation", out var op) && op?.ToString() == "digitalWrite")
            .Select(e => (Pin: e.Payload["pin"]?.ToString() ?? "", Value: e.Payload["value"]?.ToString() ?? ""));

    private static IEnumerable<string> SerialMessages(IEnumerable<SimulationEventResponse> events) =>
        events.Where(e => e.Type == "serial").Select(e => e.Payload["message"]?.ToString() ?? "");

    [Fact]
    public async Task BoolFromDigitalRead_IfVariable_TakesTrueBranch_WhenPulledUpHigh()
    {
        var events = await RunAsync("""
            const int SENSOR_PIN = 32;
            const int LED_PIN = 13;
            void setup() {
              pinMode(SENSOR_PIN, INPUT_PULLUP);
              pinMode(LED_PIN, OUTPUT);
            }
            void loop() {
              bool detected = (digitalRead(SENSOR_PIN) == HIGH);
              if (detected) {
                digitalWrite(LED_PIN, HIGH);
              } else {
                digitalWrite(LED_PIN, LOW);
              }
              delay(10);
            }
            """);

        var writes = DigitalWrites(events).ToList();
        Assert.NotEmpty(writes);
        Assert.All(writes, w => Assert.Equal("HIGH", w.Value));
    }

    [Fact]
    public async Task BoolFromDigitalRead_IfVariable_TakesFalseBranch_WhenPlainInputLow()
    {
        var events = await RunAsync("""
            const int SENSOR_PIN = 32;
            const int LED_PIN = 13;
            void setup() {
              pinMode(SENSOR_PIN, INPUT);
              pinMode(LED_PIN, OUTPUT);
            }
            void loop() {
              bool detected = digitalRead(SENSOR_PIN) == HIGH;
              if (detected) {
                digitalWrite(LED_PIN, HIGH);
              } else {
                digitalWrite(LED_PIN, LOW);
              }
              delay(10);
            }
            """);

        var writes = DigitalWrites(events).ToList();
        Assert.NotEmpty(writes);
        Assert.All(writes, w => Assert.Equal("LOW", w.Value));
    }

    [Fact]
    public async Task BoolFromDigitalRead_NegatedIfVariable_InvertsBranch()
    {
        var events = await RunAsync("""
            const int SENSOR_PIN = 32;
            const int LED_PIN = 13;
            void setup() {
              pinMode(SENSOR_PIN, INPUT_PULLUP);
              pinMode(LED_PIN, OUTPUT);
            }
            void loop() {
              bool detected = digitalRead(SENSOR_PIN) == HIGH;
              if (!detected) {
                digitalWrite(LED_PIN, HIGH);
              } else {
                digitalWrite(LED_PIN, LOW);
              }
              delay(10);
            }
            """);

        // SENSOR_PIN is pulled HIGH -> detected=true -> !detected=false -> LOW branch.
        var writes = DigitalWrites(events).ToList();
        Assert.NotEmpty(writes);
        Assert.All(writes, w => Assert.Equal("LOW", w.Value));
    }

    [Fact]
    public async Task TernaryDigitalWrite_WithBoolAlias_WritesHigh_WhenPulledUpHigh()
    {
        var events = await RunAsync("""
            const int SENSOR_PIN = 32;
            const int LED_PIN = 13;
            const int BUZZER_PIN = 25;
            void setup() {
              pinMode(SENSOR_PIN, INPUT_PULLUP);
              pinMode(LED_PIN, OUTPUT);
              pinMode(BUZZER_PIN, OUTPUT);
            }
            void loop() {
              bool detected = (digitalRead(SENSOR_PIN) == HIGH);
              digitalWrite(LED_PIN, detected ? HIGH : LOW);
              digitalWrite(BUZZER_PIN, detected ? HIGH : LOW);
              delay(10);
            }
            """);

        var writes = DigitalWrites(events).ToList();
        Assert.Contains(writes, w => w.Pin == "13" && w.Value == "HIGH");
        Assert.Contains(writes, w => w.Pin == "25" && w.Value == "HIGH");
        Assert.DoesNotContain(writes, w => w.Value == "LOW");
    }

    [Fact]
    public async Task TernaryDigitalWrite_InlineDigitalReadCondition_NoIntermediateVariable()
    {
        var events = await RunAsync("""
            const int SENSOR_PIN = 32;
            const int LED_PIN = 13;
            void setup() {
              pinMode(SENSOR_PIN, INPUT);
              pinMode(LED_PIN, OUTPUT);
            }
            void loop() {
              digitalWrite(LED_PIN, digitalRead(SENSOR_PIN) == HIGH ? HIGH : LOW);
              delay(10);
            }
            """);

        var writes = DigitalWrites(events).ToList();
        Assert.NotEmpty(writes);
        Assert.All(writes, w => Assert.Equal("LOW", w.Value));
    }

    [Fact]
    public async Task SerialPrintVariable_BoolAlias_PrintsLiveOneOrZero_NotPlaceholder()
    {
        var events = await RunAsync("""
            const int SENSOR_PIN = 32;
            void setup() {
              pinMode(SENSOR_PIN, INPUT_PULLUP);
            }
            void loop() {
              bool detected = digitalRead(SENSOR_PIN) == HIGH;
              Serial.println(detected);
              delay(10);
            }
            """);

        var messages = SerialMessages(events).ToList();
        Assert.Contains("1", messages);
        Assert.DoesNotContain(messages, m => m.Contains("detected"));
    }

    [Fact]
    public async Task SerialPrintVariable_AnalogReadAssigned_PrintsLiveNumber_NotPlaceholder()
    {
        var events = await RunAsync("""
            const int POT_PIN = 32;
            void setup() {
            }
            void loop() {
              int value = analogRead(POT_PIN);
              Serial.println(value);
              delay(10);
            }
            """);

        var messages = SerialMessages(events).ToList();
        // No potentiometer/light-sensor component wired -> ReadAnalog's
        // documented no-match fallback is 0 — the point of this test is that
        // it's the numeric "0", not the old static "<value>" placeholder.
        Assert.Contains("0", messages);
        Assert.DoesNotContain(messages, m => m.Contains("value"));
    }

    // The exact shape of STEM_FE/src/data/virtualLabSampleExercises.ts's
    // buildAlertStarterCode() template — shared by 6 sample labs (Water
    // Leak/Flame Sensor/PIR/Rain/Soil Moisture, all "Bai 8/9/11/12/13").
    // Serial.println(detected ? "ALERT" : "NORMAL") is a ternary of STRING
    // LITERALS (not HIGH/LOW), a separate gap from the digitalWrite-ternary
    // one above — without this, it would print the literal placeholder
    // "<detected ? "ALERT" : "NORMAL">" every time, not silence, but still
    // permanently wrong.
    [Fact]
    public async Task SerialPrintlnTernary_WithBoolAlias_PrintsCorrectStringLiteral_ForBothBranches()
    {
        var trueBranchEvents = await RunAsync("""
            const int SENSOR_PIN = 32;
            void setup() {
              pinMode(SENSOR_PIN, INPUT_PULLUP);
            }
            void loop() {
              bool detected = digitalRead(SENSOR_PIN) == HIGH;
              Serial.println(detected ? "CANH BAO: PHAT HIEN LUA!" : "Binh thuong: khong co lua");
              delay(10);
            }
            """);
        var trueMessages = SerialMessages(trueBranchEvents).ToList();
        Assert.Contains("CANH BAO: PHAT HIEN LUA!", trueMessages);
        Assert.DoesNotContain("Binh thuong: khong co lua", trueMessages);
        Assert.DoesNotContain(trueMessages, m => m.Contains("detected"));

        var falseBranchEvents = await RunAsync("""
            const int SENSOR_PIN = 32;
            void setup() {
              pinMode(SENSOR_PIN, INPUT);
            }
            void loop() {
              bool detected = digitalRead(SENSOR_PIN) == HIGH;
              Serial.println(detected ? "CANH BAO: PHAT HIEN LUA!" : "Binh thuong: khong co lua");
              delay(10);
            }
            """);
        var falseMessages = SerialMessages(falseBranchEvents).ToList();
        Assert.Contains("Binh thuong: khong co lua", falseMessages);
        Assert.DoesNotContain("CANH BAO: PHAT HIEN LUA!", falseMessages);
    }

    // STEM_FE's DHT station template ("Bai 10: Tram do nhiet do do am DHT")
    // does "float temperature = dht.readTemperature(); ... Serial.print(temperature);"
    // — a bare-identifier Serial arg sourced from a DhtReadAssign, not an
    // AnalogReadAssign. Same placeholder bug, different assignment source;
    // DhtReadAssign writes into the same AnalogLocals slot at runtime (see
    // EducationalEventGenerator), so registering its varname into
    // numericVarNames too is what makes this resolve live.
    [Fact]
    public async Task SerialPrintVariable_DhtReadAssigned_PrintsLiveNumber_NotPlaceholder()
    {
        var events = await RunAsync("""
            #include "StemFlowDHT.h"
            StemFlowDHT dht("dht1");
            void setup() {
            }
            void loop() {
              float temperature = dht.readTemperature();
              Serial.print(temperature);
              delay(10);
            }
            """);

        var messages = SerialMessages(events).ToList();
        // No sensorScenario configured for "dht1" -> ReadDhtScenario's
        // documented no-scenario fallback is the default 25.0C, rounded to
        // an int (AnalogLocals is int-only) -> "25", not "<temperature>".
        Assert.Contains("25", messages);
        Assert.DoesNotContain(messages, m => m.Contains("temperature"));
    }
}
