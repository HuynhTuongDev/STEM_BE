using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Users;

namespace STEM.Core.Entities.Classes;

[Index(nameof(ScheduleId), nameof(StudentId), IsUnique = true)]
public class AttendanceRecord : BaseEntity
{
    public int ClassId { get; set; }
    public int? ScheduleId { get; set; }
    public int StudentId { get; set; }
    public DateOnly AttendanceDate { get; set; }
    public string? Status { get; set; }
    public string? Note { get; set; }
    public int? MarkedById { get; set; }

    public Class? Class { get; set; }
    public Schedule? Schedule { get; set; }
    public User? Student { get; set; }
    public User? MarkedBy { get; set; }
}

public static class AttendanceStatuses
{
    public const string Present = "Present";
    public const string Absent = "Absent";

    public static bool IsValid(string? status)
    {
        return Normalize(status) != null;
    }

    public static string? Normalize(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        var normalized = status.Trim();

        if (normalized.Equals(Present, StringComparison.OrdinalIgnoreCase))
        {
            return Present;
        }

        if (normalized.Equals(Absent, StringComparison.OrdinalIgnoreCase))
        {
            return Absent;
        }

        return null;
    }
}
