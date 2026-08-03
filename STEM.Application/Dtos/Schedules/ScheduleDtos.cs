namespace STEM.Application.Dtos.Schedules;

public class CreateScheduleRequest
{
    public int ClassId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}

public class UpdateScheduleRequest
{
    public int Id { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}

public class ScheduleResponse
{
    public int Id { get; set; }
    public int ClassId { get; set; }
    public string ClassCode { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ScheduleCalendarResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string ClassCode { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string Color { get; set; } = "#3b82f6";
}

public class GetScheduleRequest
{
    public int? ClassId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

// Weekly Schedule Grid DTO
public class WeeklyScheduleGridResponse
{
    public int Year { get; set; }
    public int WeekNumber { get; set; }
    public DateTime WeekStartDate { get; set; }
    public DateTime WeekEndDate { get; set; }
    public List<DaySchedule> Days { get; set; } = new();
}

public class DaySchedule
{
    public int DayOfWeek { get; set; } // 1 = Monday, 7 = Sunday
    public string DayName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public bool IsToday { get; set; }
    public List<SlotSchedule> Slots { get; set; } = new();
}

public class SlotSchedule
{
    public int SlotNumber { get; set; }
    public string SlotName { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public List<ScheduleItem> Items { get; set; } = new();
}

public class ScheduleItem
{
    public int Id { get; set; }
    public string ClassCode { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
}