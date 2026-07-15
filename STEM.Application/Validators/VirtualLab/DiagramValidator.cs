using System.Text.Json;
using System.Linq;
using System.Collections.Generic;

namespace STEM.Application.Validators.VirtualLab;

public class DiagramValidator
{
    public (bool IsValid, List<string> Errors) Validate(JsonElement diagram)
    {
        var errors = new List<string>();

        if (!diagram.TryGetProperty("parts", out var parts) || parts.ValueKind != JsonValueKind.Array)
        {
            errors.Add("parts is required and must be an array.");
            return (false, errors);
        }

        var partIds = new HashSet<string>();
        var validTypes = new[] { "board-esp32-devkit-c-v4", "wokwi-led", "wokwi-resistor", "wokwi-pushbutton", "board-bmp180" };

        foreach (var part in parts.EnumerateArray())
        {
            if (!part.TryGetProperty("id", out var idProp) || string.IsNullOrWhiteSpace(idProp.GetString()))
            {
                errors.Add("A part is missing 'id'.");
                continue;
            }

            var id = idProp.GetString()!;
            if (!partIds.Add(id))
            {
                errors.Add($"Duplicate part id: {id}");
            }

            if (!part.TryGetProperty("type", out var typeProp) || !validTypes.Contains(typeProp.GetString()))
            {
                errors.Add($"{id}: invalid or unsupported part type.");
            }

            if (!part.TryGetProperty("top", out _) || !part.TryGetProperty("left", out _))
            {
                errors.Add($"{id}: missing top/left position.");
            }

            if (typeProp.GetString() == "board-bmp180")
            {
                if (part.TryGetProperty("attrs", out var attrs))
                {
                    if (attrs.TryGetProperty("temperature", out var tempProp))
                    {
                        var tempStr = tempProp.ValueKind == JsonValueKind.String ? tempProp.GetString() : tempProp.GetRawText();
                        if (double.TryParse(tempStr, out var temp))
                        {
                            if (temp < -40 || temp > 85) errors.Add($"{id}: temperature must be between -40 and 85");
                        }
                        else errors.Add($"{id}: temperature must be a valid number");
                    }
                    if (attrs.TryGetProperty("pressure", out var pressProp))
                    {
                        var pressStr = pressProp.ValueKind == JsonValueKind.String ? pressProp.GetString() : pressProp.GetRawText();
                        if (double.TryParse(pressStr, out var press))
                        {
                            if (press < 30000 || press > 110000) errors.Add($"{id}: pressure must be between 30000 and 110000");
                        }
                        else errors.Add($"{id}: pressure must be a valid number");
                    }
                }
            }
        }

        if (diagram.TryGetProperty("connections", out var connections) && connections.ValueKind == JsonValueKind.Array)
        {
            var seenConns = new HashSet<string>();
            foreach (var conn in connections.EnumerateArray())
            {
                if (conn.GetArrayLength() < 2) continue;
                var p1 = conn[0].GetString()!;
                var p2 = conn[1].GetString()!;
                
                var partId1 = p1.Split(':')[0];
                var partId2 = p2.Split(':')[0];

                if (!partIds.Contains(partId1)) errors.Add($"Connection references invalid part: {partId1}");
                if (!partIds.Contains(partId2)) errors.Add($"Connection references invalid part: {partId2}");

                var connId1 = $"{p1}-{p2}";
                var connId2 = $"{p2}-{p1}";
                if (seenConns.Contains(connId1) || seenConns.Contains(connId2))
                {
                    errors.Add($"Duplicate connection between {p1} and {p2}");
                }
                else
                {
                    seenConns.Add(connId1);
                }
            }
        }

        return (errors.Count == 0, errors);
    }
}
