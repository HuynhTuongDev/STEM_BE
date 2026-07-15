namespace STEM.Core.Entities.Simulations;

public class VirtualLabProject
{
    public Guid Id { get; set; }
    public int? UserId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Board { get; set; } = "esp32";
    public string Language { get; set; } = "arduino";

    public string CodeContent { get; set; } = string.Empty;
    public string DiagramJson { get; set; } = string.Empty;
    public string LibrariesJson { get; set; } = "{}";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
