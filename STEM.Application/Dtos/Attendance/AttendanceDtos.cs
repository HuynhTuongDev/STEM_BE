namespace STEM.Application.Dtos.Attendance;

public class CreateAttendanceRequest
{
    public int ClassId { get; set; }
    public int? ScheduleId { get; set; }
    public DateOnly AttendanceDate { get; set; }
    public List<CreateAttendanceRecordRequest> Records { get; set; } = new();
}

public class CreateAttendanceRecordRequest
{
    public int StudentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public class UpdateAttendanceRequest
{
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public class GetAttendanceRequest
{
    public int? ClassId { get; set; }
    public int? StudentId { get; set; }
    public DateOnly? AttendanceDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class AttendanceResponse
{
    public int Id { get; set; }
    public int ClassId { get; set; }
    public int? ScheduleId { get; set; }
    public string ClassCode { get; set; } = string.Empty;
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public DateOnly AttendanceDate { get; set; }
    public string? Status { get; set; }
    public string? Note { get; set; }
    public int? MarkedById { get; set; }
    public string? MarkedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateAttendanceResponse
{
    public int CreatedCount { get; set; }
    public IReadOnlyCollection<AttendanceResponse> Items { get; set; } = Array.Empty<AttendanceResponse>();
}

public class PagedAttendanceResponse
{
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public IReadOnlyCollection<AttendanceResponse> Items { get; set; } = Array.Empty<AttendanceResponse>();
}
