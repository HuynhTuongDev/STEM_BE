using STEM.Application.UseCases.Components.Abstractions;

namespace STEM.Application.UseCases.Components.Providers.KiCad;

// Parses one DEF...ENDDEF block from a real KiCad legacy library file
// (github.com/KiCad/kicad-symbols, *.lib — verified against a live fetch of
// Device.lib's "LED"/"Buzzer" symbols and Switch.lib's "SW_Push" symbol,
// 2026-08). Format (documented, plain-text, not XML):
//   DEF <name> <refPrefix> ...
//   F1 "<value>" ...        (human-readable name — usually == <name>)
//   X <pinName> <pinNumber> <x> <y> <length> <dir> <numSz> <nameSz> <part> <convert> <electricalType>
//   ENDDEF
// Pin lines are the ONLY thing read for pin candidates — X <pinName> is a
// real electrical pin identity here (unlike Fritzing's svgId), but is still
// only ever surfaced as a visual/candidate pin, never simulation identity.
//
// Category is deliberately resolved via a small exact-name allow-list, not
// a substring/fuzzy match — e.g. "APA-106-F5" (a real 4-pin RGB/addressable
// LED symbol in the same library) must NOT be categorized as "LED" just
// because it lives in LED.lib; its pins (DO/GND/VDD/DI) don't even use
// anode/cathode naming. Unrecognized symbols get Category = null, which
// SimulationTypeResolver already treats as NotMapped — the safe default.
public static class KiCadPartParser
{
    private static readonly IReadOnlyDictionary<string, string> KnownSymbolCategories =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["LED"] = "LED",
            ["Buzzer"] = "Buzzer",
            ["SW_Push"] = "BUTTON",
        };

    public static ExternalComponentCandidate? Parse(string defBlock, string libraryFile, string sourceUrl)
    {
        if (string.IsNullOrWhiteSpace(defBlock))
        {
            return null;
        }

        var lines = defBlock.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var defLine = lines.FirstOrDefault(l => l.StartsWith("DEF ", StringComparison.Ordinal));
        if (defLine == null)
        {
            return null;
        }

        var defParts = defLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (defParts.Length < 2)
        {
            return null;
        }

        var symbolName = defParts[1];

        var pins = lines
            .Where(l => l.StartsWith("X ", StringComparison.Ordinal))
            .Select(ParsePinLine)
            .Where(pin => pin != null)
            .Select(pin => pin!)
            .ToList();

        KnownSymbolCategories.TryGetValue(symbolName, out var category);

        return new ExternalComponentCandidate(
            Provider: KiCadConstants.ProviderName,
            ExternalId: $"{libraryFile}#{symbolName}",
            Name: symbolName,
            Category: category,
            SourceUrl: sourceUrl,
            License: "CC-BY-SA-4.0", // verified real value, see kicad-symbols/LICENSE.md
            ExternalVersion: null,   // legacy .lib files carry no per-symbol version
            Checksum: null,
            Pins: pins,
            PrimaryAssetUrl: null);
    }

    // Extracts every top-level "DEF <name> ... ENDDEF" block from a whole
    // library file — one file holds many symbols (unlike Fritzing's 1
    // file = 1 part), so search has to split before matching.
    public static IReadOnlyCollection<(string Name, string Block)> SplitDefinitions(string libraryContent)
    {
        var results = new List<(string, string)>();
        var lines = libraryContent.Split('\n');
        List<string>? current = null;
        string? currentName = null;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r');
            if (line.StartsWith("DEF ", StringComparison.Ordinal))
            {
                current = new List<string> { line };
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                currentName = parts.Length > 1 ? parts[1] : null;
                continue;
            }

            if (current == null)
            {
                continue;
            }

            current.Add(line);

            if (line.StartsWith("ENDDEF", StringComparison.Ordinal))
            {
                if (currentName != null)
                {
                    results.Add((currentName, string.Join('\n', current)));
                }

                current = null;
                currentName = null;
            }
        }

        return results;
    }

    private static ExternalPinCandidate? ParsePinLine(string line)
    {
        var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        // X <name> <number> <x> <y> <length> <dir> <numSize> <nameSize> <part> <convert> <electricalType>
        if (tokens.Length < 3)
        {
            return null;
        }

        var pinName = tokens[1];
        var pinNumber = tokens[2];
        return new ExternalPinCandidate(VisualPinId: pinNumber, Name: pinName);
    }
}

public static class KiCadConstants
{
    public const string ProviderName = "kicad";
}
