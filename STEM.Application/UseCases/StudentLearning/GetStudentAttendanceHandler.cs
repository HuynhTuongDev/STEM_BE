using STEM.Application.Dtos.StudentLearning;
using STEM.Core.Entities.Classes;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.StudentLearning;

public class GetStudentAttendanceHandler
{
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IUserRepository _userRepository;

    public GetStudentAttendanceHandler(
        IAttendanceRepository attendanceRepository,
        IUserRepository userRepository)
    {
        _attendanceRepository = attendanceRepository;
        _userRepository = userRepository;
    }

    public async Task<StudentAttendanceResponse> Handle(
        GetStudentAttendanceRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        StudentLearningGuard.EnsureStudent(currentUser);

        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100);

        var attendanceRecords = request.ClassId.HasValue
            ? await _attendanceRepository.GetByClassAndStudentAsync(request.ClassId.Value, currentUserId, cancellationToken)
            : await _attendanceRepository.GetByStudentIdAsync(currentUserId, cancellationToken);

        var filteredRecords = attendanceRecords.AsEnumerable();

        if (request.FromDate.HasValue)
        {
            filteredRecords = filteredRecords.Where(record => record.AttendanceDate >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            filteredRecords = filteredRecords.Where(record => record.AttendanceDate <= request.ToDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = request.Status.Trim();
            filteredRecords = filteredRecords.Where(record => record.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
        }

        var orderedRecords = filteredRecords
            .OrderByDescending(record => record.AttendanceDate)
            .ThenByDescending(record => record.Id)
            .ToList();

        var totalCount = orderedRecords.Count;
        var presentCount = orderedRecords.Count(IsPresent);

        return new StudentAttendanceResponse
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            PresentCount = presentCount,
            AbsentCount = orderedRecords.Count(IsAbsent),
            AttendanceRate = totalCount == 0 ? 0 : Math.Round(presentCount * 100m / totalCount, 2),
            Items = orderedRecords
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(MapToResponse)
                .ToList()
        };
    }

    private static StudentAttendanceItemResponse MapToResponse(Attendance attendance)
    {
        return new StudentAttendanceItemResponse
        {
            Id = attendance.Id,
            ClassId = attendance.ClassId,
            ClassCode = attendance.Class?.ClassCode ?? string.Empty,
            CourseId = attendance.Class?.CourseId,
            CourseTitle = attendance.Class?.Course?.Title,
            TeacherName = attendance.Class?.Teacher?.FullName,
            ScheduleId = attendance.ScheduleId,
            ScheduleStartTime = attendance.Schedule?.StartTime,
            ScheduleEndTime = attendance.Schedule?.EndTime,
            AttendanceDate = attendance.AttendanceDate,
            Status = attendance.Status,
            Note = attendance.Note
        };
    }

    private static bool IsPresent(Attendance attendance)
    {
        return attendance.Status.Equals("Present", StringComparison.OrdinalIgnoreCase)
            || attendance.Status.Equals("Attended", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAbsent(Attendance attendance)
    {
        return attendance.Status.Equals("Absent", StringComparison.OrdinalIgnoreCase);
    }
}
