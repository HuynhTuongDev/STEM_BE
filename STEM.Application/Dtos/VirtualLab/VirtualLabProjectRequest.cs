using System.Text.Json;

namespace STEM.Application.Dtos.VirtualLab;

public class VirtualLabProjectRequest
{
    public Guid? LabId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Board { get; set; } = "esp32";
    public string Language { get; set; } = "arduino";
    public string Code { get; set; } = string.Empty;
    public JsonElement Diagram { get; set; }
}
