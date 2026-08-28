using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Courses;

namespace STEM.Core.Entities.Simulations;

public class LabClassAssignment
{
    public Guid Id { get; set; }
    public Guid LabId { get; set; }
    public int ClassId { get; set; }
    public int? ScheduleId { get; set; }  // Lab này dùng cho buổi dạy nào trong lớp (nullable)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Lab? Lab { get; set; }
    public Class? Class { get; set; }
    public Schedule? Schedule { get; set; }
}
