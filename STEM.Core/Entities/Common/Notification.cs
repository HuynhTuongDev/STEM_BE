using STEM.Core.Entities.Users;

namespace STEM.Core.Entities.Common;

public class Notification : BaseEntity
{
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Type { get; set; } // e.g., "GradeReport", "AttendanceWarning", "System"
    public bool IsRead { get; set; } = false;

    public User? User { get; set; }
}
