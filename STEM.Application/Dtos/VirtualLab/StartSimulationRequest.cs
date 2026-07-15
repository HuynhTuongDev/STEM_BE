using System.Text.Json;

namespace STEM.Application.Dtos.VirtualLab;

public class StartSimulationRequest
{
    public string Code { get; set; } = string.Empty;
    public JsonElement Diagram { get; set; }
}
