using System.Text.RegularExpressions;

namespace STEM.Application.UseCases.Simulation.Runners.Educational;

public sealed class EducationalProgramAnalyzer
{
    private static readonly Regex DefineRegex = new(@"^\s*#define\s+(?<name>[A-Za-z_]\w*)\s+(?<value>[A-Za-z0-9_]+)", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex IntConstRegex = new(@"\b(?:const\s+|constexpr\s+)?(?:int|byte|uint8_t)\s+(?<name>[A-Za-z_]\w*)\s*=\s*(?<value>\d+)\s*;", RegexOptions.Compiled);
    private static readonly Regex ServoDeclarationRegex = new(@"\bServo\s+(?<name>[A-Za-z_]\w*)\s*;", RegexOptions.Compiled);
    private static readonly Regex PinModeRegex = new(@"\bpinMode\s*\(\s*(?<pin>[^,\)]+)\s*,\s*(?<mode>INPUT_PULLUP|INPUT|OUTPUT)\s*\)", RegexOptions.Compiled);
    private static readonly Regex DigitalWriteRegex = new(@"\bdigitalWrite\s*\(\s*(?<pin>[^,\)]+)\s*,\s*(?<value>HIGH|LOW|true|false|1|0)\s*\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex DigitalReadRegex = new(@"\bdigitalRead\s*\(\s*(?<pin>[^\)]+)\s*\)", RegexOptions.Compiled);
    private static readonly Regex DelayRegex = new(@"\bdelay\s*\(\s*(?<ms>[A-Za-z_]\w*|\d+)\s*\)", RegexOptions.Compiled);
    private static readonly Regex SerialRegex = new(@"\bSerial\.(?<method>begin|print|println)\s*\((?<arg>[^\)]*)\)", RegexOptions.Compiled);
    private static readonly Regex ToneRegex = new(@"\btone\s*\(\s*(?<pin>[^,\)]+)\s*,\s*(?<frequency>[^,\)]+)(?:,\s*(?<duration>[^\)]+))?\)", RegexOptions.Compiled);
    private static readonly Regex NoToneRegex = new(@"\bnoTone\s*\(\s*(?<pin>[^\)]+)\s*\)", RegexOptions.Compiled);
    private static readonly Regex AnalogWriteRegex = new(@"\banalogWrite\s*\(\s*(?<pin>[^,\)]+)\s*,\s*(?<value>[A-Za-z_]\w*|\d+)\s*\)", RegexOptions.Compiled);
    private static readonly Regex ServoAttachRegex = new(@"\b(?<name>[A-Za-z_]\w*)\s*\.\s*attach\s*\(\s*(?<pin>[^,\)]+)", RegexOptions.Compiled);
    private static readonly Regex ServoWriteRegex = new(@"\b(?<name>[A-Za-z_]\w*)\s*\.\s*write\s*\(\s*(?<angle>[A-Za-z_]\w*|\d+)\s*\)", RegexOptions.Compiled);
    private static readonly Regex ForLoopRegex = new(
        @"^\s*(?:(?:int|long|byte|uint8_t|size_t)\s+)?(?<variable>[A-Za-z_]\w*)\s*=\s*(?<start>[A-Za-z_]\w*|\d+)\s*;\s*\k<variable>\s*(?<comparison><=|<)\s*(?<end>[A-Za-z_]\w*|\d+)\s*;\s*(?:\k<variable>\s*\+\+|\+\+\s*\k<variable>|\k<variable>\s*\+=\s*1)\s*$",
        RegexOptions.Compiled);

    public EducationalProgram Analyze(string sourceCode)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(sourceCode))
        {
            errors.Add("sourceCode is required.");
            return new EducationalProgram(
                Array.Empty<EducationalInstruction>(),
                Array.Empty<EducationalInstruction>(),
                errors,
                warnings);
        }

        var cleanedSource = StripBlockComments(sourceCode);
        var symbols = BuildSymbolTable(cleanedSource);
        var servoNames = ServoDeclarationRegex
            .Matches(cleanedSource)
            .Select(match => match.Groups["name"].Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var setupBody = ExtractFunctionBody(cleanedSource, "setup");
        var loopBody = ExtractFunctionBody(cleanedSource, "loop");

        if (setupBody == null)
        {
            warnings.Add("setup() was not found.");
        }

        if (loopBody == null)
        {
            warnings.Add("loop() was not found.");
        }

        return new EducationalProgram(
            ParseInstructions(setupBody ?? string.Empty, symbols, servoNames, warnings),
            ParseInstructions(loopBody ?? string.Empty, symbols, servoNames, warnings),
            errors,
            warnings);
    }

    private static IReadOnlyList<EducationalInstruction> ParseInstructions(
        string body,
        IReadOnlyDictionary<string, string> symbols,
        IReadOnlySet<string> servoNames,
        ICollection<string> warnings)
    {
        var instructions = new List<EducationalInstruction>();
        var source = StripLineComments(body);
        var segmentStart = 0;
        var index = 0;
        var depth = 0;

        while (index < source.Length)
        {
            if (source[index] == '{')
            {
                depth++;
                index++;
                continue;
            }

            if (source[index] == '}')
            {
                depth = Math.Max(0, depth - 1);
                index++;
                continue;
            }

            if (depth == 0 &&
                (TryReadControlBlock(source, index, "for", out var header, out var controlBody, out var nextIndex) ||
                 TryReadControlBlock(source, index, "while", out header, out controlBody, out nextIndex)))
            {
                instructions.AddRange(ParseFlatInstructions(
                    source[segmentStart..index], symbols, servoNames, warnings));

                if (source.AsSpan(index).StartsWith("for", StringComparison.Ordinal))
                {
                    if (TryGetForIterationCount(header, symbols, out var iterationCount))
                    {
                        instructions.Add(EducationalInstruction.CountedLoop(
                            ParseInstructions(controlBody, symbols, servoNames, warnings),
                            iterationCount));
                    }
                    else
                    {
                        warnings.Add($"Unsupported for loop: for ({header.Trim()}).");
                    }
                }
                else if (header.Trim() is "true" or "1")
                {
                    instructions.Add(EducationalInstruction.ForeverLoop(
                        ParseInstructions(controlBody, symbols, servoNames, warnings)));
                }
                else
                {
                    warnings.Add($"Unsupported while condition: {header.Trim()}.");
                }

                index = nextIndex;
                segmentStart = nextIndex;
                continue;
            }

            index++;
        }

        instructions.AddRange(ParseFlatInstructions(source[segmentStart..], symbols, servoNames, warnings));
        return instructions;
    }

    private static IReadOnlyList<EducationalInstruction> ParseFlatInstructions(
        string body,
        IReadOnlyDictionary<string, string> symbols,
        IReadOnlySet<string> servoNames,
        ICollection<string> warnings)
    {
        var instructions = new List<EducationalInstruction>();
        var statements = StripLineComments(body)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var statement in statements)
        {
            var serial = SerialRegex.Match(statement);
            if (serial.Success)
            {
                instructions.Add(EducationalInstruction.Serial(
                    ExtractSerialMessage(serial.Groups["arg"].Value),
                    serial.Groups["method"].Value.Equals("println", StringComparison.OrdinalIgnoreCase)));
                continue;
            }

            var pinMode = PinModeRegex.Match(statement);
            if (pinMode.Success)
            {
                instructions.Add(EducationalInstruction.PinMode(
                    NormalizePin(Resolve(pinMode.Groups["pin"].Value, symbols)),
                    pinMode.Groups["mode"].Value.ToUpperInvariant()));
                continue;
            }

            var digitalRead = DigitalReadRegex.Match(statement);
            if (digitalRead.Success)
            {
                instructions.Add(EducationalInstruction.DigitalRead(
                    NormalizePin(Resolve(digitalRead.Groups["pin"].Value, symbols))));
                continue;
            }

            var digitalWrite = DigitalWriteRegex.Match(statement);
            if (digitalWrite.Success)
            {
                instructions.Add(EducationalInstruction.DigitalWrite(
                    NormalizePin(Resolve(digitalWrite.Groups["pin"].Value, symbols)),
                    NormalizeDigitalValue(digitalWrite.Groups["value"].Value)));
                continue;
            }

            var delay = DelayRegex.Match(statement);
            if (delay.Success)
            {
                if (TryResolveInt(delay.Groups["ms"].Value, symbols, out var ms))
                {
                    instructions.Add(EducationalInstruction.Delay(ms));
                }
                else
                {
                    warnings.Add($"Unsupported delay value: {delay.Groups["ms"].Value.Trim()}.");
                }

                continue;
            }

            var tone = ToneRegex.Match(statement);
            if (tone.Success)
            {
                var duration = 0;
                _ = tone.Groups["duration"].Success &&
                    TryResolveInt(tone.Groups["duration"].Value, symbols, out duration);

                if (TryResolveInt(tone.Groups["frequency"].Value, symbols, out var frequency))
                {
                    instructions.Add(EducationalInstruction.Tone(
                        NormalizePin(Resolve(tone.Groups["pin"].Value, symbols)),
                        frequency,
                        duration));
                }
                else
                {
                    warnings.Add($"Unsupported tone frequency: {tone.Groups["frequency"].Value.Trim()}.");
                }

                continue;
            }

            var noTone = NoToneRegex.Match(statement);
            if (noTone.Success)
            {
                instructions.Add(EducationalInstruction.NoTone(
                    NormalizePin(Resolve(noTone.Groups["pin"].Value, symbols))));
                continue;
            }

            var analogWrite = AnalogWriteRegex.Match(statement);
            if (analogWrite.Success)
            {
                if (TryResolveInt(analogWrite.Groups["value"].Value, symbols, out var value))
                {
                    instructions.Add(EducationalInstruction.AnalogWrite(
                        NormalizePin(Resolve(analogWrite.Groups["pin"].Value, symbols)),
                        value));
                }
                else
                {
                    warnings.Add($"Unsupported analogWrite value: {analogWrite.Groups["value"].Value.Trim()}.");
                }

                continue;
            }

            var servoAttach = ServoAttachRegex.Match(statement);
            if (servoAttach.Success && servoNames.Contains(servoAttach.Groups["name"].Value))
            {
                instructions.Add(EducationalInstruction.ServoAttach(
                    servoAttach.Groups["name"].Value,
                    NormalizePin(Resolve(servoAttach.Groups["pin"].Value, symbols))));
                continue;
            }

            var servoWrite = ServoWriteRegex.Match(statement);
            if (servoWrite.Success && servoNames.Contains(servoWrite.Groups["name"].Value))
            {
                if (TryResolveInt(servoWrite.Groups["angle"].Value, symbols, out var angle))
                {
                    instructions.Add(EducationalInstruction.ServoWrite(
                        servoWrite.Groups["name"].Value,
                        angle));
                }
                else
                {
                    warnings.Add($"Unsupported servo angle: {servoWrite.Groups["angle"].Value.Trim()}.");
                }
            }
        }

        return instructions;
    }

    private static bool TryGetForIterationCount(
        string header,
        IReadOnlyDictionary<string, string> symbols,
        out int iterationCount)
    {
        var match = ForLoopRegex.Match(header);
        if (!match.Success ||
            !TryResolveInt(match.Groups["start"].Value, symbols, out var start) ||
            !TryResolveInt(match.Groups["end"].Value, symbols, out var end))
        {
            iterationCount = 0;
            return false;
        }

        var inclusive = match.Groups["comparison"].Value == "<=";
        var count = (long)end - start + (inclusive ? 1L : 0L);
        iterationCount = (int)Math.Clamp(count, 0L, int.MaxValue);
        return true;
    }

    private static bool TryReadControlBlock(
        string source,
        int startIndex,
        string keyword,
        out string header,
        out string body,
        out int nextIndex)
    {
        header = string.Empty;
        body = string.Empty;
        nextIndex = startIndex;

        if (!source.AsSpan(startIndex).StartsWith(keyword, StringComparison.Ordinal) ||
            (startIndex + keyword.Length < source.Length &&
             (char.IsLetterOrDigit(source[startIndex + keyword.Length]) || source[startIndex + keyword.Length] == '_')))
        {
            return false;
        }

        var openParenthesis = startIndex + keyword.Length;
        while (openParenthesis < source.Length && char.IsWhiteSpace(source[openParenthesis]))
        {
            openParenthesis++;
        }

        if (openParenthesis >= source.Length || source[openParenthesis] != '(' ||
            !TryFindMatchingDelimiter(source, openParenthesis, '(', ')', out var closeParenthesis))
        {
            return false;
        }

        var openBrace = closeParenthesis + 1;
        while (openBrace < source.Length && char.IsWhiteSpace(source[openBrace]))
        {
            openBrace++;
        }

        if (openBrace >= source.Length || source[openBrace] != '{' ||
            !TryFindMatchingDelimiter(source, openBrace, '{', '}', out var closeBrace))
        {
            return false;
        }

        header = source[(openParenthesis + 1)..closeParenthesis];
        body = source[(openBrace + 1)..closeBrace];
        nextIndex = closeBrace + 1;
        return true;
    }

    private static bool TryFindMatchingDelimiter(
        string source,
        int openIndex,
        char openDelimiter,
        char closeDelimiter,
        out int closeIndex)
    {
        var depth = 0;
        for (var index = openIndex; index < source.Length; index++)
        {
            if (source[index] == openDelimiter)
            {
                depth++;
            }
            else if (source[index] == closeDelimiter && --depth == 0)
            {
                closeIndex = index;
                return true;
            }
        }

        closeIndex = -1;
        return false;
    }

    private static Dictionary<string, string> BuildSymbolTable(string sourceCode)
    {
        var symbols = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match define in DefineRegex.Matches(sourceCode))
        {
            symbols[define.Groups["name"].Value] = define.Groups["value"].Value;
        }

        foreach (Match intConst in IntConstRegex.Matches(sourceCode))
        {
            symbols[intConst.Groups["name"].Value] = intConst.Groups["value"].Value;
        }

        return symbols;
    }

    private static string? ExtractFunctionBody(string sourceCode, string functionName)
    {
        var match = Regex.Match(
            sourceCode,
            $@"\bvoid\s+{Regex.Escape(functionName)}\s*\(\s*\)\s*\{{",
            RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return null;
        }

        var openBrace = sourceCode.IndexOf('{', match.Index);
        var depth = 0;
        for (var index = openBrace; index < sourceCode.Length; index++)
        {
            if (sourceCode[index] == '{')
            {
                depth++;
            }
            else if (sourceCode[index] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return sourceCode[(openBrace + 1)..index];
                }
            }
        }

        return null;
    }

    private static string StripBlockComments(string sourceCode)
    {
        return Regex.Replace(sourceCode, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
    }

    private static string StripLineComments(string sourceCode)
    {
        return Regex.Replace(sourceCode, @"//.*?$", string.Empty, RegexOptions.Multiline);
    }

    private static string Resolve(string value, IReadOnlyDictionary<string, string> symbols)
    {
        var trimmed = value.Trim();
        return symbols.TryGetValue(trimmed, out var resolved) ? resolved : trimmed;
    }

    private static bool TryResolveInt(
        string value,
        IReadOnlyDictionary<string, string> symbols,
        out int result)
    {
        return int.TryParse(Resolve(value, symbols), out result);
    }

    private static string NormalizePin(string value)
    {
        var pin = value.Trim();
        return pin.StartsWith("GPIO", StringComparison.OrdinalIgnoreCase)
            ? pin[4..]
            : pin;
    }

    private static string NormalizeDigitalValue(string value)
    {
        return value.Equals("HIGH", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
               value == "1"
            ? "HIGH"
            : "LOW";
    }

    private static string ExtractSerialMessage(string argument)
    {
        var trimmed = argument.Trim();
        if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[^1] == '"')
        {
            return trimmed[1..^1];
        }

        return string.IsNullOrWhiteSpace(trimmed) ? string.Empty : $"<{trimmed}>";
    }
}

public sealed record EducationalProgram(
    IReadOnlyList<EducationalInstruction> SetupInstructions,
    IReadOnlyList<EducationalInstruction> LoopInstructions,
    IReadOnlyCollection<string> Errors,
    IReadOnlyCollection<string> Warnings);

public sealed record EducationalInstruction(
    EducationalInstructionKind Kind,
    string? Pin = null,
    string? Value = null,
    int NumericValue = 0,
    int DurationMs = 0,
    string? Message = null,
    bool Newline = false,
    string? ServoName = null,
    IReadOnlyList<EducationalInstruction>? Body = null,
    int IterationCount = 0)
{
    public static EducationalInstruction PinMode(string pin, string mode) =>
        new(EducationalInstructionKind.PinMode, Pin: pin, Value: mode);

    public static EducationalInstruction DigitalWrite(string pin, string value) =>
        new(EducationalInstructionKind.DigitalWrite, Pin: pin, Value: value);

    public static EducationalInstruction DigitalRead(string pin) =>
        new(EducationalInstructionKind.DigitalRead, Pin: pin);

    public static EducationalInstruction Delay(int durationMs) =>
        new(EducationalInstructionKind.Delay, DurationMs: Math.Max(0, durationMs));

    public static EducationalInstruction Serial(string message, bool newline) =>
        new(EducationalInstructionKind.Serial, Message: message, Newline: newline);

    public static EducationalInstruction Tone(string pin, int frequency, int durationMs) =>
        new(EducationalInstructionKind.Tone, Pin: pin, NumericValue: frequency, DurationMs: Math.Max(0, durationMs));

    public static EducationalInstruction NoTone(string pin) =>
        new(EducationalInstructionKind.NoTone, Pin: pin);

    public static EducationalInstruction AnalogWrite(string pin, int value) =>
        new(EducationalInstructionKind.AnalogWrite, Pin: pin, NumericValue: Math.Clamp(value, 0, 255));

    public static EducationalInstruction ServoAttach(string servoName, string pin) =>
        new(EducationalInstructionKind.ServoAttach, Pin: pin, ServoName: servoName);

    public static EducationalInstruction ServoWrite(string servoName, int angle) =>
        new(EducationalInstructionKind.ServoWrite, NumericValue: Math.Clamp(angle, 0, 180), ServoName: servoName);

    public static EducationalInstruction CountedLoop(
        IReadOnlyList<EducationalInstruction> body,
        int iterationCount) =>
        new(EducationalInstructionKind.CountedLoop, Body: body, IterationCount: Math.Max(0, iterationCount));

    public static EducationalInstruction ForeverLoop(IReadOnlyList<EducationalInstruction> body) =>
        new(EducationalInstructionKind.ForeverLoop, Body: body);
}

public enum EducationalInstructionKind
{
    PinMode,
    DigitalWrite,
    DigitalRead,
    Delay,
    Serial,
    Tone,
    NoTone,
    AnalogWrite,
    ServoAttach,
    ServoWrite,
    CountedLoop,
    ForeverLoop
}
