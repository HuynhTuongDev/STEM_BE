namespace STEM.Application.Dtos.Students;

public class StudentClassProgressResponse
{
    public int ClassId { get; set; }
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public int TeacherId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalLessons { get; set; }
    public int TotalAssignments { get; set; }
    public bool HasCertificate { get; set; }
    public int TotalAttendanceRecords { get; set; }
    public int PresentAttendanceRecords { get; set; }
    public decimal AttendanceRate { get; set; }
}
