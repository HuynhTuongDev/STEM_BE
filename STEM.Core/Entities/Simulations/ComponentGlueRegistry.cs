namespace STEM.Core.Entities.Simulations;

public class ComponentGlueRegistry
{
    public string ComponentType { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool Supported { get; set; }
    public string PinRequirementsJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
