namespace STEM.Application.Dtos.Classes;

public class GetClassesRequest
{
    public string? SearchTerm { get; set; }
    public string? Status { get; set; }
    public int? CourseId { get; set; }
    public int? TeacherId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class ClassListItemResponse
{
    public int Id { get; set; }
    public string ClassCode { get; set; } = string.Empty;
    public int SchoolId { get; set; }
    public string? SchoolName { get; set; }
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public int TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public int StudentCount { get; set; }
}

public class PagedClassListResponse
{
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public List<ClassListItemResponse> Items { get; set; } = new();
}

public class ClassDetailResponse
{
    public int Id { get; set; }
    public string ClassCode { get; set; } = string.Empty;
    public int SchoolId { get; set; }
    public string? SchoolName { get; set; }
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public int TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<StudentResponse> Students { get; set; } = new();
    public List<AvailableStudentResponse> AvailableStudents { get; set; } = new();
    public List<ScheduleResponse> Schedules { get; set; } = new();
    public List<AnnouncementResponse> Announcements { get; set; } = new();
}

public class StudentResponse
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime EnrolledAt { get; set; }
}

public class AvailableStudentResponse
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Gender { get; set; }
}

public class ScheduleResponse
{
    public int Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}

public class AnnouncementResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateClassRequest
{
    public string ClassCode { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public int TeacherId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class UpdateClassRequest
{
    public string ClassCode { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public int TeacherId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
