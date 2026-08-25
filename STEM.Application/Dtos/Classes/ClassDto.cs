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
    public int GradeLevelId { get; set; }
    public string? GradeLevelName { get; set; }
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
    public int GradeLevelId { get; set; }
    public string? GradeLevelName { get; set; }
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
    public int ClassId { get; set; }
    public string ClassCode { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public int? LessonId { get; set; }
    public string? LessonTitle { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
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
    public int GradeLevelId { get; set; }
    public int CourseId { get; set; }
    public int TeacherId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class UpdateClassRequest
{
    public string ClassCode { get; set; } = string.Empty;
    public int GradeLevelId { get; set; }
    public int CourseId { get; set; }
    public int TeacherId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class RemoveStudentRequest
{
    public int StudentId { get; set; }
}

// Curriculum DTO for classes (modules + lessons)
public class ClassCurriculumResponse
{
    public int ClassId { get; set; }
    public string ClassCode { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public List<ModuleWithLessonsDto> Modules { get; set; } = new();
}

public class ModuleWithLessonsDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public int EstimatedMinutes { get; set; }
    public int LessonCount { get; set; }
    public List<LessonInCurriculumDto> Lessons { get; set; } = new();
}

public class LessonInCurriculumDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public int EstimatedMinutes { get; set; }
    public string LessonType { get; set; } = string.Empty;
    public bool HasVirtualLab { get; set; }
    public string? LabId { get; set; }
}

public class TeacherClassDetailResponse
{
    public int Id { get; set; }
    public string ClassCode { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<StudentInfoDto> Students { get; set; } = new();
}

public class StudentInfoDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime EnrolledAt { get; set; }
}
