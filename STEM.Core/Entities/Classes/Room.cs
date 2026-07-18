namespace STEM.Core.Entities.Classes;

public class Room : BaseEntity
{
    public string RoomCode { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string? Building { get; set; }
    public int? Floor { get; set; }
    public int Capacity { get; set; } = 30;
    public bool HasProjector { get; set; } = false;
    public bool HasAirConditioner { get; set; } = true;
    public string Status { get; set; } = "Available";
}
