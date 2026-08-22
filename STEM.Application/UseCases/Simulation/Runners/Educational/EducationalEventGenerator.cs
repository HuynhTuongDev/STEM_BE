using STEM.Application.Dtos.Simulation;
using STEM.Application.UseCases.Simulation.Abstractions;
using STEM.Application.UseCases.Simulation.Runtime;
using STEM.Application.UseCases.Simulation.Runners.Educational.Components;
using STEM.Application.UseCases.Simulation.Runners.Qemu;

namespace STEM.Application.UseCases.Simulation.Runners.Educational;

public sealed class EducationalEventGenerator
{
    // `onEventEmitted` được gọi đồng bộ ngay sau khi MỖI event được tính ra
    // (không gộp/batch) — cho phép caller ghi DB/broadcast realtime. `Delay`
    // dùng Task.Delay THẬT (không chỉ cộng dồn state.Time), nên hàm này chạy
    // đúng bằng khoảng thời gian thực tế của chương trình được mô phỏng —
    // caller phải tự chạy nó trong background task, KHÔNG await trực tiếp
    // trong 1 request HTTP.
    public async Task<SimulationRunResult> GenerateAsync(
        EducationalProgram program,
        VirtualLabRuntimeDiagramSnapshot diagram,
        SimulationRunContext context,
        SimulationEventEmittedCallback onEventEmitted,
        CancellationToken cancellationToken)
    {
        var state = new EducationalRunState(context, diagram);
        await EmitAsync(state, onEventEmitted, "serial", state.Time, new Dictionary<string, object?>
        {
            ["message"] = "StemFlow educational runner started."
        }, cancellationToken);

        var setupResult = await ExecuteBlockAsync(program.SetupInstructions, state, onEventEmitted, isLoop: false, cancellationToken);
        if (!setupResult.Success)
        {
            return setupResult;
        }

        if (program.LoopInstructions.Count == 0)
        {
            state.Warnings.Add("No supported loop() instructions were detected.");
            await EmitAsync(state, onEventEmitted, "part-state", state.Time, new Dictionary<string, object?>
            {
                ["state"] = "idle",
                ["message"] = "No supported Arduino IO calls were detected."
            }, cancellationToken);

            return state.ToResult(success: true);
        }

        while (state.Time < state.MaxDurationMs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var loopResult = await ExecuteBlockAsync(program.LoopInstructions, state, onEventEmitted, isLoop: true, cancellationToken);
            if (!loopResult.Success)
            {
                return loopResult;
            }

            if (state.ReachedMaxDuration)
            {
                break;
            }
        }

        if (state.ReachedMaxDuration)
        {
            state.Warnings.Add("Simulation timeline stopped at MaxDurationMs.");
        }

        return state.ToResult(success: true);
    }

    private static async Task<SimulationRunResult> ExecuteBlockAsync(
        IReadOnlyList<EducationalInstruction> instructions,
        EducationalRunState state,
        SimulationEventEmittedCallback onEventEmitted,
        bool isLoop,
        CancellationToken cancellationToken)
    {
        foreach (var instruction in instructions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (isLoop && state.Time >= state.MaxDurationMs)
            {
                state.ReachedMaxDuration = true;
                return state.ToResult(success: true);
            }

            state.InstructionCount++;
            if (state.InstructionCount > state.Context.MaxInstructionCount)
            {
                return await state.FailAsync(onEventEmitted, "MaxInstructionCount exceeded.", cancellationToken);
            }

            var instructionResult = await ExecuteInstructionAsync(
                instruction,
                state,
                onEventEmitted,
                cancellationToken);
            if (instructionResult != null)
            {
                return instructionResult;
            }
        }

        return state.ToResult(success: true);
    }

    private static async Task<SimulationRunResult?> ExecuteInstructionAsync(
        EducationalInstruction instruction,
        EducationalRunState state,
        SimulationEventEmittedCallback onEventEmitted,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        switch (instruction.Kind)
        {
            case EducationalInstructionKind.PinMode:
                state.PinModes[instruction.Pin!] = instruction.Value!;
                await EmitAsync(state, onEventEmitted, "pin-state", state.Time, new Dictionary<string, object?>
                {
                    ["pin"] = instruction.Pin,
                    ["mode"] = instruction.Value
                }, cancellationToken);
                return null;

            case EducationalInstructionKind.DigitalWrite:
                state.PinValues[instruction.Pin!] = instruction.Value!;
                await EmitAsync(state, onEventEmitted, "pin-state", state.Time, new Dictionary<string, object?>
                {
                    ["pin"] = instruction.Pin,
                    ["value"] = instruction.Value,
                    ["operation"] = "digitalWrite"
                }, cancellationToken);

                foreach (var led in state.FindLeds(instruction.Pin!))
                {
                    await EmitAsync(state, onEventEmitted, led.ToDigitalEvent(state.Time, instruction.Value!), cancellationToken);
                }

                foreach (var buzzer in state.FindBuzzers(instruction.Pin!))
                {
                    await EmitAsync(state, onEventEmitted, buzzer.ToDigitalEvent(state.Time, instruction.Value!), cancellationToken);
                }

                foreach (var relay in state.FindRelays(instruction.Pin!))
                {
                    await EmitAsync(state, onEventEmitted, relay.ToDigitalEvent(state.Time, instruction.Value!), cancellationToken);
                }

                return null;

            case EducationalInstructionKind.DigitalRead:
                var pinMode = state.PinModes.TryGetValue(instruction.Pin!, out var mode) ? mode : null;
                var button = state.FindButtons(instruction.Pin!).FirstOrDefault();
                var digitalSensor = state.FindDigitalSensors(instruction.Pin!).FirstOrDefault();
                var value = button?.Read(state.Context.ComponentInputs, pinMode) ??
                    (digitalSensor != null
                        ? ((digitalSensor.TryReadLiveInput(state.Context.ComponentInputs) ??
                            state.ReadDigitalSensorScenario(digitalSensor.PartId, digitalSensor.UseMotionField, defaultValue: false))
                           ? "HIGH" : "LOW")
                        : (pinMode?.Equals("INPUT_PULLUP", StringComparison.OrdinalIgnoreCase) == true ? "HIGH" : "LOW"));

                await EmitAsync(state, onEventEmitted, "pin-state", state.Time, new Dictionary<string, object?>
                {
                    ["pin"] = instruction.Pin,
                    ["value"] = value,
                    ["operation"] = "digitalRead"
                }, cancellationToken);
                return null;

            case EducationalInstructionKind.AnalogReadAssign:
                // Re-read live on every visit — same reasoning as DigitalRead/If
                // below, this is what lets a running loop() react to a slider
                // (or a light-sensor reading) moved via ISimulationInputChannel
                // since the last iteration. ReadAnalog doesn't care which
                // analog-capable component is on this pin.
                var analogValue = state.ReadAnalog(instruction.Pin!, state.Context.ComponentInputs);
                state.AnalogLocals[instruction.Value!] = analogValue;

                await EmitAsync(state, onEventEmitted, "pin-state", state.Time, new Dictionary<string, object?>
                {
                    ["pin"] = instruction.Pin,
                    ["value"] = analogValue,
                    ["operation"] = "analogRead"
                }, cancellationToken);
                return null;

            case EducationalInstructionKind.DhtReadAssign:
                // Pin encodes "{componentId}:{field}" — see
                // EducationalInstruction.DhtReadAssign's comment. Re-read
                // live every visit (same reasoning as AnalogReadAssign
                // above) so a scenario timeline crossing a new mark mid-run
                // is picked up on the very next loop() pass.
                var dhtParts = instruction.Pin!.Split(':', 2);
                var dhtValue = state.ReadDhtScenario(dhtParts[0], dhtParts[1]);
                // AnalogLocals is int-only (matches IfNumeric's int
                // Threshold, same 0..4095-integer world Potentiometer/
                // LightSensor already live in) — DHT values are rounded,
                // not truncated. Documented limitation, not a bug: this
                // milestone's own sensorScenario samples only use
                // whole-number temperature/humidity marks.
                state.AnalogLocals[instruction.Value!] = (int)Math.Round(dhtValue);

                await EmitAsync(state, onEventEmitted, "pin-state", state.Time, new Dictionary<string, object?>
                {
                    ["pin"] = dhtParts[0],
                    ["value"] = dhtValue,
                    ["operation"] = dhtParts[1] == "Temperature" ? "dht.readTemperature" : "dht.readHumidity"
                }, cancellationToken);
                return null;

            case EducationalInstructionKind.Delay:
                await AdvanceTimeAsync(state, instruction.DurationMs, cancellationToken);
                return null;

            case EducationalInstructionKind.Serial:
                await EmitAsync(state, onEventEmitted, "serial", state.Time, new Dictionary<string, object?>
                {
                    ["message"] = instruction.Message ?? string.Empty,
                    ["newline"] = instruction.Newline
                }, cancellationToken);
                return null;

            case EducationalInstructionKind.Tone:
                await EmitAsync(state, onEventEmitted, "pin-state", state.Time, new Dictionary<string, object?>
                {
                    ["pin"] = instruction.Pin,
                    ["operation"] = "tone",
                    ["frequency"] = instruction.NumericValue
                }, cancellationToken);

                foreach (var buzzer in state.FindBuzzers(instruction.Pin!))
                {
                    await EmitAsync(state, onEventEmitted, buzzer.ToToneEvent(state.Time, instruction.NumericValue), cancellationToken);
                }

                if (instruction.DurationMs > 0)
                {
                    await AdvanceTimeAsync(state, instruction.DurationMs, cancellationToken);
                    foreach (var buzzer in state.FindBuzzers(instruction.Pin!))
                    {
                        await EmitAsync(state, onEventEmitted, buzzer.ToSilentEvent(state.Time), cancellationToken);
                    }
                }

                return null;

            case EducationalInstructionKind.NoTone:
                await EmitAsync(state, onEventEmitted, "pin-state", state.Time, new Dictionary<string, object?>
                {
                    ["pin"] = instruction.Pin,
                    ["operation"] = "noTone"
                }, cancellationToken);

                foreach (var buzzer in state.FindBuzzers(instruction.Pin!))
                {
                    await EmitAsync(state, onEventEmitted, buzzer.ToSilentEvent(state.Time), cancellationToken);
                }

                return null;

            case EducationalInstructionKind.AnalogWrite:
                await EmitAsync(state, onEventEmitted, "pin-state", state.Time, new Dictionary<string, object?>
                {
                    ["pin"] = instruction.Pin,
                    ["value"] = instruction.NumericValue,
                    ["operation"] = "analogWrite"
                }, cancellationToken);

                foreach (var led in state.FindLeds(instruction.Pin!))
                {
                    await EmitAsync(state, onEventEmitted, led.ToPwmEvent(state.Time, instruction.NumericValue), cancellationToken);
                }

                foreach (var buzzer in state.FindBuzzers(instruction.Pin!))
                {
                    await EmitAsync(
                        state,
                        onEventEmitted,
                        instruction.NumericValue > 0
                            ? buzzer.ToToneEvent(state.Time, instruction.NumericValue)
                            : buzzer.ToSilentEvent(state.Time),
                        cancellationToken);
                }

                return null;

            case EducationalInstructionKind.ServoAttach:
                state.ServoPins[instruction.ServoName!] = instruction.Pin!;
                await EmitAsync(state, onEventEmitted, "pin-state", state.Time, new Dictionary<string, object?>
                {
                    ["pin"] = instruction.Pin,
                    ["operation"] = "servo.attach",
                    ["servo"] = instruction.ServoName
                }, cancellationToken);
                return null;

            case EducationalInstructionKind.ServoWrite:
                if (!state.ServoPins.TryGetValue(instruction.ServoName!, out var servoPin))
                {
                    state.Warnings.Add($"Servo '{instruction.ServoName}' write ignored because attach() was not detected.");
                    return null;
                }

                foreach (var servo in state.FindServos(servoPin))
                {
                    await EmitAsync(state, onEventEmitted, servo.ToAngleEvent(state.Time, instruction.NumericValue), cancellationToken);
                }

                return null;

            case EducationalInstructionKind.If:
                // Re-read live on EVERY visit (not cached from parse time) —
                // this is what lets a running loop() react to a value an
                // external caller wrote into ComponentInputs via
                // ISimulationInputChannel since the last iteration.
                IReadOnlyList<EducationalInstruction>? branch;
                if (instruction.ComparisonOperator != null)
                {
                    // Numeric mode (IfNumeric) — Pin holds the AnalogLocals
                    // variable name here, set by the AnalogReadAssign visited
                    // earlier this same loop() pass. Missing/never-assigned
                    // variable reads as 0, matching an uninitialized int's
                    // typical Arduino behavior closely enough for this scope.
                    var currentValue = state.AnalogLocals.TryGetValue(instruction.Pin!, out var storedValue)
                        ? storedValue
                        : 0;
                    var conditionTrue = instruction.ComparisonOperator switch
                    {
                        ">" => currentValue > instruction.Threshold,
                        "<" => currentValue < instruction.Threshold,
                        ">=" => currentValue >= instruction.Threshold,
                        "<=" => currentValue <= instruction.Threshold,
                        _ => false
                    };
                    branch = conditionTrue ? instruction.Body : instruction.ElseBody;
                }
                else
                {
                    var conditionPinMode = state.PinModes.TryGetValue(instruction.Pin!, out var ifMode) ? ifMode : null;
                    var conditionButton = state.FindButtons(instruction.Pin!).FirstOrDefault();
                    var conditionDigitalSensor = state.FindDigitalSensors(instruction.Pin!).FirstOrDefault();
                    var actualValue = conditionButton?.Read(state.Context.ComponentInputs, conditionPinMode) ??
                        (conditionDigitalSensor != null
                            ? ((conditionDigitalSensor.TryReadLiveInput(state.Context.ComponentInputs) ??
                                state.ReadDigitalSensorScenario(conditionDigitalSensor.PartId, conditionDigitalSensor.UseMotionField, defaultValue: false))
                               ? "HIGH" : "LOW")
                            : (conditionPinMode?.Equals("INPUT_PULLUP", StringComparison.OrdinalIgnoreCase) == true ? "HIGH" : "LOW"));
                    branch = actualValue.Equals(instruction.Value, StringComparison.OrdinalIgnoreCase)
                        ? instruction.Body
                        : instruction.ElseBody;
                }

                if (branch == null || branch.Count == 0)
                {
                    return null;
                }

                var branchResult = await ExecuteBlockAsync(branch, state, onEventEmitted, isLoop: true, cancellationToken);
                if (!branchResult.Success || state.ReachedMaxDuration)
                {
                    return branchResult;
                }

                return null;

            case EducationalInstructionKind.CountedLoop:
                for (var iteration = 0; iteration < instruction.IterationCount; iteration++)
                {
                    var loopResult = await ExecuteBlockAsync(
                        instruction.Body ?? Array.Empty<EducationalInstruction>(),
                        state,
                        onEventEmitted,
                        isLoop: true,
                        cancellationToken);
                    if (!loopResult.Success || state.ReachedMaxDuration)
                    {
                        return loopResult;
                    }

                    state.InstructionCount++;
                    if (state.InstructionCount > state.Context.MaxInstructionCount)
                    {
                        return await state.FailAsync(
                            onEventEmitted,
                            "MaxInstructionCount exceeded.",
                            cancellationToken);
                    }
                }

                return null;

            case EducationalInstructionKind.ForeverLoop:
                while (!state.ReachedMaxDuration)
                {
                    var loopResult = await ExecuteBlockAsync(
                        instruction.Body ?? Array.Empty<EducationalInstruction>(),
                        state,
                        onEventEmitted,
                        isLoop: true,
                        cancellationToken);
                    if (!loopResult.Success || state.ReachedMaxDuration)
                    {
                        return loopResult;
                    }

                    state.InstructionCount++;
                    if (state.InstructionCount > state.Context.MaxInstructionCount)
                    {
                        return await state.FailAsync(
                            onEventEmitted,
                            "MaxInstructionCount exceeded.",
                            cancellationToken);
                    }
                }

                return state.ToResult(success: true);

            default:
                return null;
        }
    }

    // Chờ THẬT (Task.Delay, không phải cộng dồn state.Time) — đây là điểm
    // mấu chốt để toàn bộ simulation chạy đúng theo nhịp thời gian thực tế
    // của delay() trong sketch. Nếu bị hủy giữa chừng (Stop/MaxDurationMs ở
    // tầng ngoài), Task.Delay ném OperationCanceledException, dừng sạch
    // ngay tại đây — không có instruction nào chạy tiếp sau đó.
    private static async Task AdvanceTimeAsync(
        EducationalRunState state,
        int durationMs,
        CancellationToken cancellationToken)
    {
        if (durationMs <= 0)
        {
            return;
        }

        var nextTime = state.Time + durationMs;
        if (nextTime >= state.MaxDurationMs)
        {
            var remaining = state.MaxDurationMs - state.Time;
            if (remaining > 0)
            {
                await Task.Delay((int)Math.Min(remaining, int.MaxValue), cancellationToken);
            }

            state.Time = state.MaxDurationMs;
            state.ReachedMaxDuration = true;
            return;
        }

        await Task.Delay(durationMs, cancellationToken);
        state.Time = nextTime;
    }

    private static Task EmitAsync(
        EducationalRunState state,
        SimulationEventEmittedCallback onEventEmitted,
        string type,
        long time,
        IReadOnlyDictionary<string, object?> payload,
        CancellationToken cancellationToken)
    {
        return EmitAsync(state, onEventEmitted, new SimulationEventResponse
        {
            Type = type,
            Time = time,
            Payload = payload
        }, cancellationToken);
    }

    private static async Task EmitAsync(
        EducationalRunState state,
        SimulationEventEmittedCallback onEventEmitted,
        SimulationEventResponse evt,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        state.Events.Add(evt);
        await onEventEmitted(evt);
    }

    private sealed class EducationalRunState
    {
        private readonly Dictionary<string, List<LedModel>> _ledsByPin = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<ButtonModel>> _buttonsByPin = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<BuzzerModel>> _buzzersByPin = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<ServoModel>> _servosByPin = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<PotentiometerModel>> _potentiometersByPin = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<LightSensorModel>> _lightSensorsByPin = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<RelayModel>> _relaysByPin = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<DigitalSensorModel>> _digitalSensorsByPin = new(StringComparer.OrdinalIgnoreCase);

        // DHT scripted-sensor timeline — reused AS-IS from the QEMU side
        // (STEP 8/9: "Reuse trực tiếp semantic của QEMU. Không viết một
        // timeline behavior khác.") via SensorRuntimeHeaderGenerator's own
        // parser, so both runners read the exact same sensorScenario shape.
        private readonly SensorScenarioConfig? _scenario;

        // Must match SensorRuntimeHeaderGenerator.cs's DefaultTemperatureC/
        // DefaultHumidityPct exactly (those are private consts there) — kept
        // in sync by SensorRuntimeHeaderGeneratorDhtTests.cs's read-assign
        // coverage plus this port's own DhtEducationalTests.
        private const double DefaultTemperatureC = 25.0;
        private const double DefaultHumidityPct = 50.0;

        public EducationalRunState(SimulationRunContext context, VirtualLabRuntimeDiagramSnapshot diagram)
        {
            Context = context;
            MaxDurationMs = Math.Max(1, context.MaxDurationMs);
            BuildComponentIndexes(diagram);
            _scenario = SensorRuntimeHeaderGenerator.TryParseScenario(context.DiagramJson);
        }

        public SimulationRunContext Context { get; }
        public long Time { get; set; }
        public int InstructionCount { get; set; }
        public int MaxDurationMs { get; }
        public bool ReachedMaxDuration { get; set; }
        public List<SimulationEventResponse> Events { get; } = new();
        public List<string> Errors { get; } = new();
        public List<string> Warnings { get; } = new();
        public Dictionary<string, string> PinModes { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> PinValues { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> ServoPins { get; } = new(StringComparer.OrdinalIgnoreCase);

        // Minimal local-variable slot for "int value = analogRead(pin);" followed
        // later by "if (value > N)" — NOT a general variable system (no arithmetic,
        // no other types). Overwritten by the SAME AnalogReadAssign instruction
        // every loop() iteration, so it's always fresh by the time an If reads it —
        // no separate per-iteration reset needed.
        public Dictionary<string, int> AnalogLocals { get; } = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyCollection<LedModel> FindLeds(string pin) => Find(_ledsByPin, pin);
        public IReadOnlyCollection<ButtonModel> FindButtons(string pin) => Find(_buttonsByPin, pin);
        public IReadOnlyCollection<BuzzerModel> FindBuzzers(string pin) => Find(_buzzersByPin, pin);
        public IReadOnlyCollection<ServoModel> FindServos(string pin) => Find(_servosByPin, pin);
        public IReadOnlyCollection<PotentiometerModel> FindPotentiometers(string pin) => Find(_potentiometersByPin, pin);
        public IReadOnlyCollection<LightSensorModel> FindLightSensors(string pin) => Find(_lightSensorsByPin, pin);
        public IReadOnlyCollection<RelayModel> FindRelays(string pin) => Find(_relaysByPin, pin);
        public IReadOnlyCollection<DigitalSensorModel> FindDigitalSensors(string pin) => Find(_digitalSensorsByPin, pin);

        // analogRead() doesn't care WHAT is attached to the pin, only that
        // something producing a 0..4095 value is — same reasoning as
        // digitalRead() not caring whether it's reading a button vs. some
        // other digital source. Tries every analog-capable component pool.
        public int ReadAnalog(string pin, IReadOnlyDictionary<string, object> componentInputs)
        {
            var pot = FindPotentiometers(pin).FirstOrDefault();
            if (pot != null) return pot.Read(componentInputs);

            var lightSensor = FindLightSensors(pin).FirstOrDefault();
            if (lightSensor != null) return lightSensor.Read(componentInputs);

            return 0;
        }

        // DHT step-function timeline lookup — direct C# port of
        // SensorRuntimeHeaderGenerator.cs's __sf_lookupFloat, same semantics:
        // "latest scenario entry whose TimeMs <= now" (elapsed simulated
        // time, this class's own Time — the Educational equivalent of
        // QEMU's millis()), stepping forward only, no interpolation. Before
        // the first entry, returns the FIRST entry's value (matching
        // __sf_lookupFloat exactly) — defaultValue only applies when the
        // component has no scenario/timeline entries for this field at all.
        public double ReadDhtScenario(string componentId, string field)
        {
            var defaultValue = field == "Temperature" ? DefaultTemperatureC : DefaultHumidityPct;
            if (_scenario == null || !_scenario.Sensors.TryGetValue(componentId, out var timeline))
            {
                return defaultValue;
            }

            var entries = timeline.Timeline
                .Where(e => field == "Temperature" ? e.Temperature.HasValue : e.Humidity.HasValue)
                .OrderBy(e => e.TimeMs)
                .Select(e => (e.TimeMs, Value: (field == "Temperature" ? e.Temperature : e.Humidity)!.Value))
                .ToList();

            if (entries.Count == 0)
            {
                return defaultValue;
            }

            var result = entries[0].Value;
            foreach (var entry in entries)
            {
                if (entry.TimeMs <= Time) result = entry.Value; else break;
            }

            return result;
        }

        // Generic digital-sensor scripted-scenario lookup — same step-function
        // semantics as ReadDhtScenario, ported for the boolean-valued sensor
        // family (PIR's Motion field; Water Leak/Flame/Soil Moisture/Rain/
        // Vibration/IR Obstacle's shared Detected field — see
        // SensorTimelineEntry's own doc comments in SensorScenarioDtos.cs).
        // Deliberately NOT extended to HC-SR04 (needs pulseIn/microsecond
        // arithmetic) or Line Tracking (needs pattern-to-per-channel-array
        // logic) — QEMU-only remains the correct, honest state for those two,
        // matching STEP 8/17's explicit low-ROI/runner-honesty guidance.
        public bool ReadDigitalSensorScenario(string componentId, bool useMotionField, bool defaultValue)
        {
            if (_scenario == null || !_scenario.Sensors.TryGetValue(componentId, out var timeline))
            {
                return defaultValue;
            }

            var entries = timeline.Timeline
                .Where(e => useMotionField ? e.Motion.HasValue : e.Detected.HasValue)
                .OrderBy(e => e.TimeMs)
                .Select(e => (e.TimeMs, Value: (useMotionField ? e.Motion : e.Detected)!.Value))
                .ToList();

            if (entries.Count == 0)
            {
                return defaultValue;
            }

            var result = entries[0].Value;
            foreach (var entry in entries)
            {
                if (entry.TimeMs <= Time) result = entry.Value; else break;
            }

            return result;
        }

        public SimulationRunResult ToResult(bool success)
        {
            return new SimulationRunResult
            {
                Success = success && Errors.Count == 0,
                Events = Events.ToList(),
                Errors = Errors.ToList(),
                Warnings = Warnings.ToList()
            };
        }

        public async Task<SimulationRunResult> FailAsync(
            SimulationEventEmittedCallback onEventEmitted,
            string message,
            CancellationToken cancellationToken)
        {
            Errors.Add(message);
            await EmitAsync(this, onEventEmitted, "error", Time, new Dictionary<string, object?>
            {
                ["message"] = message
            }, cancellationToken);

            return ToResult(success: false);
        }

        private void BuildComponentIndexes(VirtualLabRuntimeDiagramSnapshot diagram)
        {
            foreach (var component in diagram.Components)
            {
                if (component.Type.Equals("wokwi-led", StringComparison.OrdinalIgnoreCase) &&
                    TryFindPin(component, new[] { "A" }, out var ledPin))
                {
                    AddModel(_ledsByPin, ledPin, new LedModel(component.Id, ledPin));
                }
                else if (component.Type.Equals("wokwi-pushbutton", StringComparison.OrdinalIgnoreCase) &&
                         TryFindPin(component, new[] { "1.l", "2.l", "1.r", "2.r" }, out var buttonPin))
                {
                    AddModel(_buttonsByPin, buttonPin, new ButtonModel(component.Id, buttonPin));
                }
                else if (component.Type.Equals("wokwi-buzzer", StringComparison.OrdinalIgnoreCase) &&
                         TryFindPin(component, new[] { "1", "2" }, out var buzzerPin))
                {
                    AddModel(_buzzersByPin, buzzerPin, new BuzzerModel(component.Id, buzzerPin));
                }
                else if (component.Type.Equals("wokwi-servo", StringComparison.OrdinalIgnoreCase) &&
                         TryFindPin(component, new[] { "PWM" }, out var servoPin))
                {
                    AddModel(_servosByPin, servoPin, new ServoModel(component.Id, servoPin));
                }
                else if (component.Type.Equals("wokwi-potentiometer", StringComparison.OrdinalIgnoreCase) &&
                         TryFindPin(component, new[] { "SIG" }, out var potPin))
                {
                    AddModel(_potentiometersByPin, potPin, new PotentiometerModel(component.Id, potPin));
                }
                else if (component.Type.Equals("wokwi-photoresistor-sensor", StringComparison.OrdinalIgnoreCase) &&
                         TryFindPin(component, new[] { "AO" }, out var lightPin))
                {
                    AddModel(_lightSensorsByPin, lightPin, new LightSensorModel(component.Id, lightPin));
                }
                else if (component.Type.Equals("wokwi-relay-module", StringComparison.OrdinalIgnoreCase) &&
                         TryFindPin(component, new[] { "IN" }, out var relayPin))
                {
                    AddModel(_relaysByPin, relayPin, new RelayModel(component.Id, relayPin));
                }
                else if (component.Type.Equals("wokwi-pir-motion-sensor", StringComparison.OrdinalIgnoreCase) &&
                         TryFindPin(component, new[] { "OUT" }, out var pirPin))
                {
                    AddModel(_digitalSensorsByPin, pirPin, new DigitalSensorModel(component.Id, pirPin, useMotionField: true));
                }
                else if (component.Type.Equals("wokwi-water-leak-sensor", StringComparison.OrdinalIgnoreCase) &&
                         TryFindPin(component, new[] { "S" }, out var waterLeakPin))
                {
                    AddModel(_digitalSensorsByPin, waterLeakPin, new DigitalSensorModel(component.Id, waterLeakPin, useMotionField: false));
                }
                else if (component.Type.Equals("wokwi-flame-sensor", StringComparison.OrdinalIgnoreCase) &&
                         TryFindPin(component, new[] { "DOUT" }, out var flamePin))
                {
                    AddModel(_digitalSensorsByPin, flamePin, new DigitalSensorModel(component.Id, flamePin, useMotionField: false));
                }
                else if (component.Type.Equals("wokwi-soil-moisture-sensor", StringComparison.OrdinalIgnoreCase) &&
                         TryFindPin(component, new[] { "DO" }, out var soilPin))
                {
                    AddModel(_digitalSensorsByPin, soilPin, new DigitalSensorModel(component.Id, soilPin, useMotionField: false));
                }
                else if (component.Type.Equals("wokwi-rain-sensor", StringComparison.OrdinalIgnoreCase) &&
                         TryFindPin(component, new[] { "DO" }, out var rainPin))
                {
                    AddModel(_digitalSensorsByPin, rainPin, new DigitalSensorModel(component.Id, rainPin, useMotionField: false));
                }
                else if (component.Type.Equals("wokwi-vibration-sensor", StringComparison.OrdinalIgnoreCase) &&
                         TryFindPin(component, new[] { "OUT" }, out var vibrationPin))
                {
                    AddModel(_digitalSensorsByPin, vibrationPin, new DigitalSensorModel(component.Id, vibrationPin, useMotionField: false));
                }
                else if (component.Type.Equals("wokwi-ir-obstacle-sensor", StringComparison.OrdinalIgnoreCase) &&
                         TryFindPin(component, new[] { "OUT" }, out var irObstaclePin))
                {
                    AddModel(_digitalSensorsByPin, irObstaclePin, new DigitalSensorModel(component.Id, irObstaclePin, useMotionField: false));
                }
            }
        }

        private static bool TryFindPin(
            VirtualLabRuntimeComponent component,
            IReadOnlyCollection<string> candidatePins,
            out string gpioPin)
        {
            foreach (var pin in candidatePins)
            {
                if (component.PinToGpio.TryGetValue(pin, out gpioPin!))
                {
                    return true;
                }
            }

            gpioPin = component.PinToGpio.Values.FirstOrDefault() ?? string.Empty;
            return gpioPin.Length > 0;
        }

        private static IReadOnlyCollection<T> Find<T>(
            IReadOnlyDictionary<string, List<T>> index,
            string pin)
        {
            return index.TryGetValue(pin, out var matches) ? matches : Array.Empty<T>();
        }

        private static void AddModel<T>(Dictionary<string, List<T>> index, string pin, T model)
        {
            if (!index.TryGetValue(pin, out var models))
            {
                models = new List<T>();
                index[pin] = models;
            }

            models.Add(model);
        }
    }
}
