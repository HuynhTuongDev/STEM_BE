using System.Text.RegularExpressions;

namespace STEM.Application.UseCases.Simulation.Runners.Educational;

public sealed class EducationalProgramAnalyzer
{
    private static readonly Regex DefineRegex = new(@"^\s*#define\s+(?<name>[A-Za-z_]\w*)\s+(?<value>[A-Za-z0-9_]+)", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex IntConstRegex = new(@"\b(?:const\s+|constexpr\s+)?(?:int|byte|uint8_t)\s+(?<name>[A-Za-z_]\w*)\s*=\s*(?<value>\d+)\s*;", RegexOptions.Compiled);
    // Threshold consts declared as float/double (e.g. "const float
    // TEMP_THRESHOLD_C = 35.0;" — the shipped DHT sample exercise's exact
    // shape). The numeric-If path (IfNumericConditionRegex/TryResolveInt)
    // only understands integer thresholds, matching the same 0..4095
    // integer world Potentiometer/LightSensor already live in — so the
    // value is rounded to the nearest int right here, at symbol-table build
    // time, rather than teaching TryResolveInt (used by delay/tone/servo
    // too) to parse decimals. Narrow, isolated, does not touch any other
    // threshold-parsing path.
    private static readonly Regex FloatConstRegex = new(@"\b(?:const\s+|constexpr\s+)?(?:float|double)\s+(?<name>[A-Za-z_]\w*)\s*=\s*(?<value>\d+(?:\.\d+)?)\s*;", RegexOptions.Compiled);
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
    // Minimal on purpose (STEP 4/9 of the realtime-input vertical slice): only
    // "if (digitalRead(PIN))" / "if (digitalRead(PIN) == HIGH|LOW|true|false|1|0)"
    // with an optional leading "!" — not a general expression evaluator. This is
    // exactly the shape Arduino sketches use to react to a button, which is the
    // one thing this interpreter needed to actually branch on live input.
    private static readonly Regex IfDigitalReadConditionRegex = new(
        @"^\s*(?<negate>!\s*)?digitalRead\s*\(\s*(?<pin>[^\)]+)\s*\)\s*(?:==\s*(?<value>HIGH|LOW|true|false|1|0)\s*)?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    // "int value = analogRead(PIN);" — the ONE assignment shape this
    // interpreter understands, matching the exact idiom analog sketches use
    // (analogRead's result almost always gets read once and compared later,
    // never inlined). Not a general variable system: no arithmetic, no other
    // r-value expressions, one scalar per name, overwritten fresh every
    // loop() visit.
    private static readonly Regex AnalogReadAssignRegex = new(
        @"^\s*(?:int|long|uint16_t)\s+(?<name>[A-Za-z_]\w*)\s*=\s*analogRead\s*\(\s*(?<pin>[^\)]+)\s*\)\s*$",
        RegexOptions.Compiled);
    private static readonly Regex AnalogReadRegex = new(@"\banalogRead\s*\(\s*(?<pin>[^\)]+)\s*\)", RegexOptions.Compiled);
    // "if (<variableName> >|<|>=|<= <threshold>)" — the numeric counterpart to
    // IfDigitalReadConditionRegex above, for a variable previously set by
    // AnalogReadAssignRegex. Threshold must be a literal or a known #define/
    // const int symbol (resolved via Resolve()), same as everywhere else in
    // this file — no arbitrary expressions on either side.
    private static readonly Regex IfNumericConditionRegex = new(
        @"^\s*(?<name>[A-Za-z_]\w*)\s*(?<op>>=|<=|>|<)\s*(?<threshold>[A-Za-z_]\w*|\d+)\s*$",
        RegexOptions.Compiled);
    // DHT22/DHT11 scripted-sensor port (QEMU already supports this exact
    // contract via SensorRuntimeHeaderGenerator.cs — see that file's
    // StemFlowDHT class). "StemFlowDHT dht("dht1");" — a top-level
    // declaration, same shape/role as ServoDeclarationRegex above: maps a
    // sketch-local variable name to a diagram componentId (the sensorScenario
    // key), not a pin.
    private static readonly Regex DhtDeclarationRegex = new(
        @"\bStemFlowDHT\s+(?<name>[A-Za-z_]\w*)\s*\(\s*""(?<id>[^""]*)""\s*\)\s*;",
        RegexOptions.Compiled);
    // "float temperature = dht.readTemperature();" / "...readHumidity();" —
    // the ONLY two method calls supported on a declared StemFlowDHT variable
    // (STEP 6: no generic object.method() engine). Deliberately narrower than
    // ServoAttach/WriteRegex (those don't require a specific declared-name
    // check at the regex level, only via servoNames.Contains after matching)
    // — here the exact same gating happens via dhtNames.ContainsKey below.
    private static readonly Regex DhtReadAssignRegex = new(
        @"^\s*(?:float|double)\s+(?<varname>[A-Za-z_]\w*)\s*=\s*(?<dhtname>[A-Za-z_]\w*)\s*\.\s*read(?<field>Temperature|Humidity)\s*\(\s*\)\s*$",
        RegexOptions.Compiled);

    // EDUCATIONAL SYNTAX COMPATIBILITY HARDENING (2026-08-26): the shipped
    // Flame Sensor sample ("bool detected = (digitalRead(PIN) == HIGH); ...
    // if (detected) ...; digitalWrite(PIN, detected ? HIGH : LOW);") compiled
    // fine but silently produced ZERO loop() instructions — none of the
    // three shapes below existed. Rather than build a real variable/
    // expression system, this adds ONE narrow symbolic alias: a `bool` local
    // assigned from a `digitalRead(pin)` comparison is recorded as
    // (name -> pin, expectedValueForTrue) and later USES of that name in
    // `if (name)`/`if (!name)` or a `cond ? HIGH : LOW` digitalWrite are
    // rewritten, at parse time, into the exact same digitalRead-condition
    // shape IfDigitalReadConditionRegex already produces — zero new runtime
    // instruction kinds, zero new execution paths, same live-read semantics.
    // "bool x = (digitalRead(PIN) == HIGH);" and "bool x = digitalRead(PIN);"
    // (bare, HIGH-is-true) both match; the wrapping "( ... )" is optional.
    private static readonly Regex BoolDigitalReadAssignRegex = new(
        @"^\s*(?:const\s+)?bool\s+(?<name>[A-Za-z_]\w*)\s*=\s*\(?\s*digitalRead\s*\(\s*(?<pin>[^\)]+)\s*\)\s*(?:==\s*(?<value>HIGH|LOW|true|false|1|0)\s*)?\)?\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    // "if (name)" / "if (!name)" where name is a BoolDigitalReadAssignRegex
    // alias — deliberately just a bare identifier, no operators, so it never
    // collides with IfDigitalReadConditionRegex (literal "digitalRead(...)"
    // text) or IfNumericConditionRegex (requires a comparison operator).
    private static readonly Regex IfBoolAliasConditionRegex = new(
        @"^\s*(?<negate>!\s*)?(?<name>[A-Za-z_]\w*)\s*$",
        RegexOptions.Compiled);
    // "digitalWrite(pin, aliasName ? HIGH : LOW)" (optionally negated) — the
    // ternary counterpart of IfBoolAliasConditionRegex above. aliasName must
    // resolve via the same boolAliases table or this simply doesn't match
    // (falls through to the existing unsupported-statement path, same as
    // today — no regression for names that aren't a known digitalRead alias).
    private static readonly Regex DigitalWriteTernaryAliasRegex = new(
        @"^\s*digitalWrite\s*\(\s*(?<pin>[^,]+?)\s*,\s*(?<negate>!\s*)?(?<name>[A-Za-z_]\w*)\s*\?\s*(?<trueval>HIGH|LOW|true|false|1|0)\s*:\s*(?<falseval>HIGH|LOW|true|false|1|0)\s*\)\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    // "digitalWrite(pin, digitalRead(sensorPin) [== HIGH] ? HIGH : LOW)" — the
    // inline-condition counterpart (no intermediate bool variable at all).
    private static readonly Regex DigitalWriteTernaryInlineRegex = new(
        @"^\s*digitalWrite\s*\(\s*(?<pin>[^,]+?)\s*,\s*\(?\s*(?<negate>!\s*)?digitalRead\s*\(\s*(?<condpin>[^\)]+)\s*\)\s*(?:==\s*(?<condvalue>HIGH|LOW|true|false|1|0)\s*)?\)?\s*\?\s*(?<trueval>HIGH|LOW|true|false|1|0)\s*:\s*(?<falseval>HIGH|LOW|true|false|1|0)\s*\)\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    // Serial.print(variableName)/Serial.println(variableName) with a BARE
    // identifier argument (no quotes, no operators) — previously baked into
    // a static "<name>" placeholder string at parse time (never the actual
    // runtime value). Matched separately from SerialRegex above so the
    // analyzer can tell "string literal" apart from "identifier to resolve
    // live" before falling back to the old placeholder behavior.
    private static readonly Regex SerialVariableArgRegex = new(
        @"^\s*[A-Za-z_]\w*\s*$",
        RegexOptions.Compiled);
    // Serial.print/println(aliasName ? "msgIfTrue" : "msgIfFalse") — the
    // shipped alert-sensor sample family's actual shape ("Bai 8/9/11/12/13":
    // Water Leak/Flame/PIR/Rain/Soil Moisture all share this exact
    // buildAlertStarterCode() template). Distinct from the HIGH/LOW-valued
    // DigitalWriteTernary*Regex above — here the two ternary branches are
    // STRING LITERALS, so this rewrites into an If wrapping two Serial
    // instructions instead of two DigitalWrite instructions. Without this,
    // SerialRegex below still "matches" (its arg capture is permissive) but
    // ExtractSerialMessage's placeholder fallback prints the literal source
    // text every time — worse than silence, a permanently-wrong message.
    private static readonly Regex SerialTernaryAliasRegex = new(
        @"^\s*Serial\.(?<method>print|println)\s*\(\s*(?<negate>!\s*)?(?<name>[A-Za-z_]\w*)\s*\?\s*""(?<truemsg>[^""]*)""\s*:\s*""(?<falsemsg>[^""]*)""\s*\)\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    // Same rewrite, inline "digitalRead(pin) [== HIGH]" condition instead of
    // a named bool alias — symmetry with DigitalWriteTernaryInlineRegex.
    private static readonly Regex SerialTernaryInlineRegex = new(
        @"^\s*Serial\.(?<method>print|println)\s*\(\s*\(?\s*(?<negate>!\s*)?digitalRead\s*\(\s*(?<condpin>[^\)]+)\s*\)\s*(?:==\s*(?<condvalue>HIGH|LOW|true|false|1|0)\s*)?\)?\s*\?\s*""(?<truemsg>[^""]*)""\s*:\s*""(?<falsemsg>[^""]*)""\s*\)\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

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
        // name -> componentId (the sensorScenario key), not a HashSet like
        // servoNames — DhtReadAssign needs the componentId, not just a
        // presence check.
        var dhtNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in DhtDeclarationRegex.Matches(cleanedSource))
        {
            dhtNames[match.Groups["name"].Value] = match.Groups["id"].Value;
        }

        // Built INCREMENTALLY, in true source order, by ParseFlatInstructions
        // itself as it walks setup()/loop() (see the BoolDigitalReadAssignRegex
        // and AnalogReadAssignRegex handling there) — NOT pre-scanned as a
        // separate whole-source pass. A naive whole-source split on ';' (like
        // servoNames/dhtNames use, which is safe for THEM because \b-based
        // regexes don't care what text surrounds a match) would mix in
        // brace-only text from unrelated function boundaries for THESE two
        // anchored (^...$) regexes — e.g. "}\nvoid loop() {\n  bool detected =
        // ..." all as one segment, silently never matching. A single mutable
        // Dictionary/HashSet threaded through the same recursive parse (and
        // shared by reference across nested if/for/while bodies) means a
        // usage sees exactly the declarations that textually precede it,
        // matching how a person reads the code top-to-bottom — still no real
        // scoping (a name declared inside one if-branch stays visible after
        // it), consistent with everything else this interpreter already does.
        var boolAliases = new Dictionary<string, (string Pin, string ExpectedValue)>(StringComparer.OrdinalIgnoreCase);
        var numericVarNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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
            ParseInstructions(setupBody ?? string.Empty, symbols, servoNames, dhtNames, boolAliases, numericVarNames, warnings),
            ParseInstructions(loopBody ?? string.Empty, symbols, servoNames, dhtNames, boolAliases, numericVarNames, warnings),
            errors,
            warnings);
    }

    private static IReadOnlyList<EducationalInstruction> ParseInstructions(
        string body,
        IReadOnlyDictionary<string, string> symbols,
        IReadOnlySet<string> servoNames,
        IReadOnlyDictionary<string, string> dhtNames,
        Dictionary<string, (string Pin, string ExpectedValue)> boolAliases,
        HashSet<string> numericVarNames,
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
                 TryReadControlBlock(source, index, "while", out header, out controlBody, out nextIndex) ||
                 TryReadControlBlock(source, index, "if", out header, out controlBody, out nextIndex)))
            {
                instructions.AddRange(ParseFlatInstructions(
                    source[segmentStart..index], symbols, servoNames, dhtNames, boolAliases, numericVarNames, warnings));

                if (source.AsSpan(index).StartsWith("for", StringComparison.Ordinal))
                {
                    if (TryGetForIterationCount(header, symbols, out var iterationCount))
                    {
                        instructions.Add(EducationalInstruction.CountedLoop(
                            ParseInstructions(controlBody, symbols, servoNames, dhtNames, boolAliases, numericVarNames, warnings),
                            iterationCount));
                    }
                    else
                    {
                        warnings.Add($"Unsupported for loop: for ({header.Trim()}).");
                    }
                }
                else if (source.AsSpan(index).StartsWith("while", StringComparison.Ordinal))
                {
                    if (header.Trim() is "true" or "1")
                    {
                        instructions.Add(EducationalInstruction.ForeverLoop(
                            ParseInstructions(controlBody, symbols, servoNames, dhtNames, boolAliases, numericVarNames, warnings)));
                    }
                    else
                    {
                        warnings.Add($"Unsupported while condition: {header.Trim()}.");
                    }
                }
                else
                {
                    // "if" — optional "else { ... }" immediately following the
                    // if-block's closing brace is consumed here too (single
                    // level only, no "else if" chaining — kept minimal on
                    // purpose, see IfDigitalReadConditionRegex comment).
                    var conditionMatch = IfDigitalReadConditionRegex.Match(header);
                    var elseBody = string.Empty;
                    var scan = nextIndex;
                    while (scan < source.Length && char.IsWhiteSpace(source[scan]))
                    {
                        scan++;
                    }

                    if (source.AsSpan(scan).StartsWith("else", StringComparison.Ordinal) &&
                        (scan + 4 >= source.Length || !(char.IsLetterOrDigit(source[scan + 4]) || source[scan + 4] == '_')))
                    {
                        var elseBraceStart = scan + 4;
                        while (elseBraceStart < source.Length && char.IsWhiteSpace(source[elseBraceStart]))
                        {
                            elseBraceStart++;
                        }

                        if (elseBraceStart < source.Length && source[elseBraceStart] == '{' &&
                            TryFindMatchingDelimiter(source, elseBraceStart, '{', '}', out var elseBraceEnd))
                        {
                            elseBody = source[(elseBraceStart + 1)..elseBraceEnd];
                            nextIndex = elseBraceEnd + 1;
                        }
                    }

                    var numericConditionMatch = conditionMatch.Success ? null : IfNumericConditionRegex.Match(header);
                    // Only tried when neither digitalRead-inline nor numeric
                    // condition matched, AND the bare header text resolves to
                    // a known BoolDigitalReadAssignRegex alias — a plain
                    // unrelated identifier (e.g. an unsupported toggle flag)
                    // correctly falls through to the "Unsupported if
                    // condition" warning below, same as before this change.
                    var boolAliasMatch = conditionMatch.Success || (numericConditionMatch?.Success ?? false)
                        ? null
                        : IfBoolAliasConditionRegex.Match(header);
                    var resolvedAlias = boolAliasMatch != null && boolAliasMatch.Success &&
                        boolAliases.TryGetValue(boolAliasMatch.Groups["name"].Value, out var aliasFromIf)
                        ? aliasFromIf
                        : ((string Pin, string ExpectedValue)?)null;

                    if (conditionMatch.Success)
                    {
                        var pin = NormalizePin(Resolve(conditionMatch.Groups["pin"].Value, symbols));
                        var expected = conditionMatch.Groups["value"].Success
                            ? NormalizeDigitalValue(conditionMatch.Groups["value"].Value)
                            : "HIGH";
                        if (conditionMatch.Groups["negate"].Success)
                        {
                            expected = expected == "HIGH" ? "LOW" : "HIGH";
                        }

                        instructions.Add(EducationalInstruction.If(
                            pin,
                            expected,
                            ParseInstructions(controlBody, symbols, servoNames, dhtNames, boolAliases, numericVarNames, warnings),
                            ParseInstructions(elseBody, symbols, servoNames, dhtNames, boolAliases, numericVarNames, warnings)));
                    }
                    else if (numericConditionMatch != null && numericConditionMatch.Success &&
                             TryResolveInt(numericConditionMatch.Groups["threshold"].Value, symbols, out var threshold))
                    {
                        instructions.Add(EducationalInstruction.IfNumeric(
                            numericConditionMatch.Groups["name"].Value,
                            numericConditionMatch.Groups["op"].Value,
                            threshold,
                            ParseInstructions(controlBody, symbols, servoNames, dhtNames, boolAliases, numericVarNames, warnings),
                            ParseInstructions(elseBody, symbols, servoNames, dhtNames, boolAliases, numericVarNames, warnings)));
                    }
                    else if (resolvedAlias != null)
                    {
                        var expected = resolvedAlias.Value.ExpectedValue;
                        if (boolAliasMatch!.Groups["negate"].Success)
                        {
                            expected = expected == "HIGH" ? "LOW" : "HIGH";
                        }

                        instructions.Add(EducationalInstruction.If(
                            resolvedAlias.Value.Pin,
                            expected,
                            ParseInstructions(controlBody, symbols, servoNames, dhtNames, boolAliases, numericVarNames, warnings),
                            ParseInstructions(elseBody, symbols, servoNames, dhtNames, boolAliases, numericVarNames, warnings)));
                    }
                    else
                    {
                        warnings.Add($"Unsupported if condition: {header.Trim()}.");
                    }
                }

                index = nextIndex;
                segmentStart = nextIndex;
                continue;
            }

            index++;
        }

        instructions.AddRange(ParseFlatInstructions(source[segmentStart..], symbols, servoNames, dhtNames, boolAliases, numericVarNames, warnings));
        return instructions;
    }

    private static IReadOnlyList<EducationalInstruction> ParseFlatInstructions(
        string body,
        IReadOnlyDictionary<string, string> symbols,
        IReadOnlySet<string> servoNames,
        IReadOnlyDictionary<string, string> dhtNames,
        Dictionary<string, (string Pin, string ExpectedValue)> boolAliases,
        HashSet<string> numericVarNames,
        ICollection<string> warnings)
    {
        var instructions = new List<EducationalInstruction>();
        var statements = StripLineComments(body)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var statement in statements)
        {
            // The declaration itself ("bool detected = digitalRead(...) ==
            // HIGH;") has no observable side effect in real Arduino either —
            // registers into boolAliases (mutable, shared by reference with
            // every recursive ParseInstructions/ParseFlatInstructions call
            // for this whole Analyze() pass) so a LATER "if (detected)" or
            // "digitalWrite(pin, detected ? A : B)" in true source order can
            // resolve it — then emits no instruction, since a plain local
            // bool declaration has no runtime side effect to reproduce.
            var boolAliasDecl = BoolDigitalReadAssignRegex.Match(statement);
            if (boolAliasDecl.Success)
            {
                var aliasPin = NormalizePin(Resolve(boolAliasDecl.Groups["pin"].Value, symbols));
                var aliasExpected = boolAliasDecl.Groups["value"].Success
                    ? NormalizeDigitalValue(boolAliasDecl.Groups["value"].Value)
                    : "HIGH";
                boolAliases[boolAliasDecl.Groups["name"].Value] = (aliasPin, aliasExpected);
                continue;
            }

            var dhtReadAssign = DhtReadAssignRegex.Match(statement);
            if (dhtReadAssign.Success &&
                dhtNames.TryGetValue(dhtReadAssign.Groups["dhtname"].Value, out var dhtComponentId))
            {
                // A DhtReadAssign writes into the SAME AnalogLocals slot an
                // AnalogReadAssign would (see EducationalEventGenerator's
                // DhtReadAssign case) — registering the varname here too
                // means "Serial.print(temperature)" after "float temperature
                // = dht.readTemperature();" resolves to the live reading
                // instead of a static placeholder, same fix as analogRead.
                numericVarNames.Add(dhtReadAssign.Groups["varname"].Value);
                instructions.Add(EducationalInstruction.DhtReadAssign(
                    dhtComponentId,
                    dhtReadAssign.Groups["field"].Value,
                    dhtReadAssign.Groups["varname"].Value));
                continue;
            }

            // Must run before the plain SerialRegex check below: SerialRegex's
            // arg capture is permissive enough to "match" a ternary-of-strings
            // argument too, which would otherwise fall into
            // ExtractSerialMessage's placeholder branch (prints the literal
            // source text every time) instead of resolving live.
            var serialTernaryAlias = SerialTernaryAliasRegex.Match(statement);
            if (serialTernaryAlias.Success &&
                boolAliases.TryGetValue(serialTernaryAlias.Groups["name"].Value, out var serialTernaryAliasValue))
            {
                var serialTernaryNewline = serialTernaryAlias.Groups["method"].Value.Equals("println", StringComparison.OrdinalIgnoreCase);
                var serialTernaryExpected = serialTernaryAliasValue.ExpectedValue;
                if (serialTernaryAlias.Groups["negate"].Success)
                {
                    serialTernaryExpected = serialTernaryExpected == "HIGH" ? "LOW" : "HIGH";
                }

                instructions.Add(EducationalInstruction.If(
                    serialTernaryAliasValue.Pin,
                    serialTernaryExpected,
                    new[] { EducationalInstruction.Serial(serialTernaryAlias.Groups["truemsg"].Value, serialTernaryNewline) },
                    new[] { EducationalInstruction.Serial(serialTernaryAlias.Groups["falsemsg"].Value, serialTernaryNewline) }));
                continue;
            }

            var serialTernaryInline = SerialTernaryInlineRegex.Match(statement);
            if (serialTernaryInline.Success)
            {
                var serialTernaryInlineNewline = serialTernaryInline.Groups["method"].Value.Equals("println", StringComparison.OrdinalIgnoreCase);
                var serialTernaryInlineConditionPin = NormalizePin(Resolve(serialTernaryInline.Groups["condpin"].Value, symbols));
                var serialTernaryInlineExpected = serialTernaryInline.Groups["condvalue"].Success
                    ? NormalizeDigitalValue(serialTernaryInline.Groups["condvalue"].Value)
                    : "HIGH";
                if (serialTernaryInline.Groups["negate"].Success)
                {
                    serialTernaryInlineExpected = serialTernaryInlineExpected == "HIGH" ? "LOW" : "HIGH";
                }

                instructions.Add(EducationalInstruction.If(
                    serialTernaryInlineConditionPin,
                    serialTernaryInlineExpected,
                    new[] { EducationalInstruction.Serial(serialTernaryInline.Groups["truemsg"].Value, serialTernaryInlineNewline) },
                    new[] { EducationalInstruction.Serial(serialTernaryInline.Groups["falsemsg"].Value, serialTernaryInlineNewline) }));
                continue;
            }

            var serial = SerialRegex.Match(statement);
            if (serial.Success)
            {
                var rawArg = serial.Groups["arg"].Value;
                var isNewline = serial.Groups["method"].Value.Equals("println", StringComparison.OrdinalIgnoreCase);
                var variableArgMatch = SerialVariableArgRegex.Match(rawArg);
                var variableName = variableArgMatch.Success ? rawArg.Trim() : null;

                if (variableName != null && boolAliases.TryGetValue(variableName, out var serialBoolAlias))
                {
                    instructions.Add(EducationalInstruction.SerialBoolVariable(
                        serialBoolAlias.Pin, serialBoolAlias.ExpectedValue, isNewline));
                }
                else if (variableName != null && numericVarNames.Contains(variableName))
                {
                    instructions.Add(EducationalInstruction.SerialNumericVariable(variableName, isNewline));
                }
                else
                {
                    instructions.Add(EducationalInstruction.Serial(ExtractSerialMessage(rawArg), isNewline));
                }

                continue;
            }

            // "digitalWrite(pin, aliasName ? HIGH : LOW)" — rewritten into the
            // exact same shape a hand-written "if (aliasName) digitalWrite(pin,
            // HIGH); else digitalWrite(pin, LOW);" would produce, reusing the
            // existing live-read If instruction (see BoolDigitalReadAssignRegex
            // comment above) instead of adding a new instruction kind. MUST run
            // before DigitalReadRegex/AnalogReadRegex below: those use \b (not
            // anchored), so they'd otherwise match the "digitalRead(...)"
            // substring inside a ternary condition first and misparse the
            // whole statement as a bare, side-effect-only digitalRead() call.
            var ternaryAlias = DigitalWriteTernaryAliasRegex.Match(statement);
            if (ternaryAlias.Success &&
                boolAliases.TryGetValue(ternaryAlias.Groups["name"].Value, out var ternaryAliasValue))
            {
                var ternaryAliasOutPin = NormalizePin(Resolve(ternaryAlias.Groups["pin"].Value, symbols));
                var ternaryAliasExpected = ternaryAliasValue.ExpectedValue;
                if (ternaryAlias.Groups["negate"].Success)
                {
                    ternaryAliasExpected = ternaryAliasExpected == "HIGH" ? "LOW" : "HIGH";
                }

                instructions.Add(EducationalInstruction.If(
                    ternaryAliasValue.Pin,
                    ternaryAliasExpected,
                    new[] { EducationalInstruction.DigitalWrite(ternaryAliasOutPin, NormalizeDigitalValue(ternaryAlias.Groups["trueval"].Value)) },
                    new[] { EducationalInstruction.DigitalWrite(ternaryAliasOutPin, NormalizeDigitalValue(ternaryAlias.Groups["falseval"].Value)) }));
                continue;
            }

            // Same rewrite, but the condition is an inline "digitalRead(pin)
            // [== HIGH]" expression instead of a named bool alias — no
            // intermediate variable at all.
            var ternaryInline = DigitalWriteTernaryInlineRegex.Match(statement);
            if (ternaryInline.Success)
            {
                var ternaryInlineOutPin = NormalizePin(Resolve(ternaryInline.Groups["pin"].Value, symbols));
                var ternaryInlineConditionPin = NormalizePin(Resolve(ternaryInline.Groups["condpin"].Value, symbols));
                var ternaryInlineExpected = ternaryInline.Groups["condvalue"].Success
                    ? NormalizeDigitalValue(ternaryInline.Groups["condvalue"].Value)
                    : "HIGH";
                if (ternaryInline.Groups["negate"].Success)
                {
                    ternaryInlineExpected = ternaryInlineExpected == "HIGH" ? "LOW" : "HIGH";
                }

                instructions.Add(EducationalInstruction.If(
                    ternaryInlineConditionPin,
                    ternaryInlineExpected,
                    new[] { EducationalInstruction.DigitalWrite(ternaryInlineOutPin, NormalizeDigitalValue(ternaryInline.Groups["trueval"].Value)) },
                    new[] { EducationalInstruction.DigitalWrite(ternaryInlineOutPin, NormalizeDigitalValue(ternaryInline.Groups["falseval"].Value)) }));
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

            var analogReadAssign = AnalogReadAssignRegex.Match(statement);
            if (analogReadAssign.Success)
            {
                // Registered the same incremental, source-order way as
                // boolAliases above — a LATER Serial.print(name) resolves to
                // the live AnalogLocals value instead of a static placeholder.
                numericVarNames.Add(analogReadAssign.Groups["name"].Value);
                instructions.Add(EducationalInstruction.AnalogReadAssign(
                    NormalizePin(Resolve(analogReadAssign.Groups["pin"].Value, symbols)),
                    analogReadAssign.Groups["name"].Value));
                continue;
            }

            var digitalRead = DigitalReadRegex.Match(statement);
            if (digitalRead.Success)
            {
                instructions.Add(EducationalInstruction.DigitalRead(
                    NormalizePin(Resolve(digitalRead.Groups["pin"].Value, symbols))));
                continue;
            }

            var analogRead = AnalogReadRegex.Match(statement);
            if (analogRead.Success)
            {
                instructions.Add(EducationalInstruction.AnalogReadAssign(
                    NormalizePin(Resolve(analogRead.Groups["pin"].Value, symbols)),
                    "_"));
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

        foreach (Match floatConst in FloatConstRegex.Matches(sourceCode))
        {
            if (double.TryParse(
                floatConst.Groups["value"].Value,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var doubleValue))
            {
                symbols[floatConst.Groups["name"].Value] =
                    ((int)Math.Round(doubleValue)).ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
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
    int IterationCount = 0,
    IReadOnlyList<EducationalInstruction>? ElseBody = null,
    // Numeric-If only (IfNumeric factory below) — Pin/Value above stay
    // reserved for the digital-equality If.
    string? ComparisonOperator = null,
    int Threshold = 0)
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

    // Serial.print(aliasName)/Serial.println(aliasName) where aliasName is a
    // BoolDigitalReadAssignRegex alias — re-reads the pin live at emit time
    // (same reasoning as If) and prints "1"/"0", matching real Arduino's
    // Serial.print(bool) behaviour, instead of the static "<aliasName>"
    // placeholder the plain Serial() case above would otherwise bake in.
    public static EducationalInstruction SerialBoolVariable(string pin, string expectedValueForTrue, bool newline) =>
        new(EducationalInstructionKind.SerialBoolVariable, Pin: pin, Value: expectedValueForTrue, Newline: newline);

    // Serial.print(varName)/Serial.println(varName) where varName was
    // assigned via "int varName = analogRead(pin);" earlier — looks up
    // AnalogLocals live at emit time instead of baking a placeholder.
    public static EducationalInstruction SerialNumericVariable(string variableName, bool newline) =>
        new(EducationalInstructionKind.SerialNumericVariable, Value: variableName, Newline: newline);

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

    // Pin/Value here mean "the digitalRead condition to (re-)evaluate every time
    // this instruction is reached" (Pin to read, Value it must equal to take the
    // then-branch) — re-read live, not baked in at parse time, which is exactly
    // what lets a running loop() react to ISimulationInputChannel input.
    public static EducationalInstruction If(
        string pin,
        string expectedValue,
        IReadOnlyList<EducationalInstruction> thenBody,
        IReadOnlyList<EducationalInstruction> elseBody) =>
        new(EducationalInstructionKind.If, Pin: pin, Value: expectedValue, Body: thenBody, ElseBody: elseBody);

    // pin: source GPIO to read (or the sentinel "_" for a bare, unassigned
    // analogRead(pin) call kept only for its Serial/event side effect).
    // variableName: AnalogLocals key the value is stored under afterward.
    public static EducationalInstruction AnalogReadAssign(string pin, string variableName) =>
        new(EducationalInstructionKind.AnalogReadAssign, Pin: pin, Value: variableName);

    // DHT scripted-sensor read. Reuses the exact same AnalogLocals scalar
    // slot AnalogReadAssign writes to (STEP 7: no rename needed, no second
    // dictionary) — Pin encodes "{componentId}:{field}" (field is
    // "Temperature" or "Humidity") since the read source is a
    // sensorScenario timeline keyed by componentId, not a GPIO pin.
    public static EducationalInstruction DhtReadAssign(string componentId, string field, string variableName) =>
        new(EducationalInstructionKind.DhtReadAssign, Pin: $"{componentId}:{field}", Value: variableName);

    // IfNumeric reuses Pin to mean "AnalogLocals variable name to compare" —
    // distinct from If's Pin (a GPIO), disambiguated by ComparisonOperator
    // being non-null. Re-evaluated live every visit, same as If.
    public static EducationalInstruction IfNumeric(
        string variableName,
        string comparisonOperator,
        int threshold,
        IReadOnlyList<EducationalInstruction> thenBody,
        IReadOnlyList<EducationalInstruction> elseBody) =>
        new(EducationalInstructionKind.If, Pin: variableName, Body: thenBody, ElseBody: elseBody,
            ComparisonOperator: comparisonOperator, Threshold: threshold);
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
    ForeverLoop,
    If,
    AnalogReadAssign,
    DhtReadAssign,
    SerialBoolVariable,
    SerialNumericVariable
}
