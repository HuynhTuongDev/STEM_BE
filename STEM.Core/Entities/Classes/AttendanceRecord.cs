using STEM.Core.Entities.Users;

namespace STEM.Core.Entities.Classes;

public class AttendanceRecord : BaseEntity
{
    public int ClassId { get; set; }
    public int StudentId { get; set; }
    public DateOnly AttendanceDate { get; set; }
    public string Status { get; set; } = AttendanceStatuses.Present;
    public string? Note { get; set; }
    public int MarkedById { get; set; }

    public Class? Class { get; set; }
    public User? Student { get; set; }
    public User? MarkedBy { get; set; }
}

public static class AttendanceStatuses
{
    public const string Present = "Present";
    public const string Absent = "Absent";
    public const string Late = "Late";
    public const string Excused = "Excused";

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

        if (normalized.Equals(Late, StringComparison.OrdinalIgnoreCase))
        {
            return Late;
        }

        if (normalized.Equals(Excused, StringComparison.OrdinalIgnoreCase))
        {
            return Excused;
        }

        return null;
    }
}
