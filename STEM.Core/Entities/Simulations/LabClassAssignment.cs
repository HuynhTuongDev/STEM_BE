using STEM.Core.Entities.Classes;

namespace STEM.Core.Entities.Simulations;

public class LabClassAssignment
{
    public Guid Id { get; set; }
    public Guid LabId { get; set; }
    public int ClassId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Lab? Lab { get; set; }
    public Class? Class { get; set; }
}
