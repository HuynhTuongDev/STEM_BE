using STEM.Core.Entities.Users;

namespace STEM.Core.Entities.Classes;

public class Attendance : BaseEntity
{
    public int ClassId { get; set; }
    public int StudentId { get; set; }
    public int? ScheduleId { get; set; }
    public DateTime AttendanceDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }

    public Class? Class { get; set; }
    public User? Student { get; set; }
    public Schedule? Schedule { get; set; }
}
