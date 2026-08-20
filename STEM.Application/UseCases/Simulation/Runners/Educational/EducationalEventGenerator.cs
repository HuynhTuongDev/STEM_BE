using STEM.Application.Dtos.Simulation;
using STEM.Application.UseCases.Simulation.Abstractions;
using STEM.Application.UseCases.Simulation.Runtime;
using STEM.Application.UseCases.Simulation.Runners.Educational.Components;

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

                return null;

            case EducationalInstructionKind.DigitalRead:
                var pinMode = state.PinModes.TryGetValue(instruction.Pin!, out var mode) ? mode : null;
                var button = state.FindButtons(instruction.Pin!).FirstOrDefault();
                var value = button?.Read(state.Context.ComponentInputs, pinMode) ??
                    (pinMode?.Equals("INPUT_PULLUP", StringComparison.OrdinalIgnoreCase) == true ? "HIGH" : "LOW");

                await EmitAsync(state, onEventEmitted, "pin-state", state.Time, new Dictionary<string, object?>
                {
                    ["pin"] = instruction.Pin,
                    ["value"] = value,
                    ["operation"] = "digitalRead"
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
                var conditionPinMode = state.PinModes.TryGetValue(instruction.Pin!, out var ifMode) ? ifMode : null;
                var conditionButton = state.FindButtons(instruction.Pin!).FirstOrDefault();
                var actualValue = conditionButton?.Read(state.Context.ComponentInputs, conditionPinMode) ??
                    (conditionPinMode?.Equals("INPUT_PULLUP", StringComparison.OrdinalIgnoreCase) == true ? "HIGH" : "LOW");
                var branch = actualValue.Equals(instruction.Value, StringComparison.OrdinalIgnoreCase)
                    ? instruction.Body
                    : instruction.ElseBody;

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

        public EducationalRunState(SimulationRunContext context, VirtualLabRuntimeDiagramSnapshot diagram)
        {
            Context = context;
            MaxDurationMs = Math.Max(1, context.MaxDurationMs);
            BuildComponentIndexes(diagram);
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

        public IReadOnlyCollection<LedModel> FindLeds(string pin) => Find(_ledsByPin, pin);
        public IReadOnlyCollection<ButtonModel> FindButtons(string pin) => Find(_buttonsByPin, pin);
        public IReadOnlyCollection<BuzzerModel> FindBuzzers(string pin) => Find(_buzzersByPin, pin);
        public IReadOnlyCollection<ServoModel> FindServos(string pin) => Find(_servosByPin, pin);

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
