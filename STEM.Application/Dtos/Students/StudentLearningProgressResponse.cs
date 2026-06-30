namespace STEM.Application.Dtos.Students;

public class StudentLearningProgressResponse
{
    public int StudentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int TotalEnrolledClasses { get; set; }
    public int ActiveClassCount { get; set; }
    public int TotalCourses { get; set; }
    public int CompletedCourses { get; set; }
    public decimal CourseCompletionRate { get; set; }
    public int TotalLessons { get; set; }
    public int TotalAssignments { get; set; }
    public int TotalProjects { get; set; }
    public int TotalSimulationSessions { get; set; }
    public int TotalGrades { get; set; }
    public double? AverageScore { get; set; }
    public int CertificatesEarned { get; set; }
    public int TotalAttendanceRecords { get; set; }
    public int PresentAttendanceRecords { get; set; }
    public decimal AttendanceRate { get; set; }
    public IReadOnlyCollection<StudentClassProgressResponse> Classes { get; set; } = Array.Empty<StudentClassProgressResponse>();
    public IReadOnlyCollection<StudentGradeResponse> RecentGrades { get; set; } = Array.Empty<StudentGradeResponse>();
}
