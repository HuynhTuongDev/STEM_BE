using STEM.Application.Dtos.Attendance;
using STEM.Application.Interfaces;
using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Attendance;

public class CreateAttendanceHandler
{
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IClassRepository _classRepository;
    private readonly IUserRepository _userRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateAttendanceHandler(
        IAttendanceRepository attendanceRepository,
        IClassRepository classRepository,
        IUserRepository userRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _attendanceRepository = attendanceRepository;
        _classRepository = classRepository;
        _userRepository = userRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<CreateAttendanceResponse> Handle(
        CreateAttendanceRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (request.ClassId <= 0)
        {
            throw new ArgumentException("ClassId is required.");
        }

        if (request.AttendanceDate == default)
        {
            throw new ArgumentException("AttendanceDate is required.");
        }

        if (request.Records.Count == 0)
        {
            throw new ArgumentException("At least one attendance record is required.");
        }

        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
        {
            throw new UnauthorizedAccessException("User does not exist.");
        }

        var classEntity = await _classRepository.GetByIdWithDetailsAsync(request.ClassId, cancellationToken);
        if (classEntity == null)
        {
            throw new KeyNotFoundException("Class not found.");
        }

        if (!AttendanceAuthorization.CanManageClass(currentUser, classEntity))
        {
            throw new UnauthorizedAccessException("You are not allowed to create attendance for this class.");
        }

        // Teachers may only take attendance on the day the class session actually happens.
        // SchoolAdministrator is not restricted by this rule (their edit rights follow separate business needs).
        if (currentUser.Role?.Name == RoleNames.Teacher)
        {
            var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
            if (request.AttendanceDate != today)
            {
                throw new ArgumentException("Attendance can only be created on the scheduled class date.");
            }
        }

        var duplicatedStudentIds = request.Records
            .GroupBy(record => record.StudentId)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();
        if (duplicatedStudentIds.Count > 0)
        {
            throw new ArgumentException($"Duplicated student ids: {string.Join(", ", duplicatedStudentIds)}.");
        }

        var enrollmentsByStudentId = classEntity.Enrollments.ToDictionary(enrollment => enrollment.StudentId);
        var existingRecords = await _attendanceRepository.GetByClassDateAsync(
            request.ClassId,
            request.AttendanceDate,
            cancellationToken);
        var existingStudentIds = existingRecords.Select(record => record.StudentId).ToHashSet();

        var createdRecords = new List<AttendanceRecord>();
        foreach (var item in request.Records)
        {
            if (!enrollmentsByStudentId.TryGetValue(item.StudentId, out var enrollment))
            {
                throw new ArgumentException($"Student {item.StudentId} is not enrolled in this class.");
            }

            if (existingStudentIds.Contains(item.StudentId))
            {
                throw new ArgumentException($"Attendance for student {item.StudentId} on this date already exists.");
            }

            var status = AttendanceStatuses.Normalize(item.Status);
            if (status == null)
            {
                throw new ArgumentException($"Invalid attendance status for student {item.StudentId}.");
            }

            var now = DateTime.UtcNow;
            var attendanceRecord = new AttendanceRecord
            {
                ClassId = request.ClassId,
                StudentId = item.StudentId,
                AttendanceDate = request.AttendanceDate,
                Status = status,
                Note = string.IsNullOrWhiteSpace(item.Note) ? null : item.Note.Trim(),
                MarkedById = currentUser.Id,
                CreatedAt = now,
                UpdatedAt = now,
                Class = classEntity,
                Student = enrollment.Student,
                MarkedBy = currentUser
            };

            await _attendanceRepository.AddAsync(attendanceRecord, cancellationToken);
            createdRecords.Add(attendanceRecord);
        }

        await _attendanceRepository.SaveChangesAsync(cancellationToken);

        return new CreateAttendanceResponse
        {
            CreatedCount = createdRecords.Count,
            Items = createdRecords.Select(AttendanceResponseMapper.Map).ToList()
        };
    }
}
