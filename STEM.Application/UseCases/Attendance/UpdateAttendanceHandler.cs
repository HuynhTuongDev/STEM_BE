using STEM.Application.Dtos.Attendance;
using STEM.Core.Entities.Classes;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Attendance;

public class UpdateAttendanceHandler
{
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IUserRepository _userRepository;

    public UpdateAttendanceHandler(
        IAttendanceRepository attendanceRepository,
        IUserRepository userRepository)
    {
        _attendanceRepository = attendanceRepository;
        _userRepository = userRepository;
    }

    public async Task<AttendanceResponse> Handle(
        int id,
        UpdateAttendanceRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
        {
            throw new UnauthorizedAccessException("User does not exist.");
        }

        var attendanceRecord = await _attendanceRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        if (attendanceRecord == null)
        {
            throw new KeyNotFoundException("Attendance record not found.");
        }

        if (attendanceRecord.Class == null || !AttendanceAuthorization.CanManageClass(currentUser, attendanceRecord.Class))
        {
            throw new UnauthorizedAccessException("You are not allowed to update this attendance record.");
        }

        var status = AttendanceStatuses.Normalize(request.Status);
        if (status == null)
        {
            throw new ArgumentException("Invalid attendance status.");
        }

        attendanceRecord.Status = status;
        attendanceRecord.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        attendanceRecord.MarkedById = currentUser.Id;
        attendanceRecord.MarkedBy = currentUser;
        attendanceRecord.UpdatedAt = DateTime.UtcNow;

        _attendanceRepository.Update(attendanceRecord);
        await _attendanceRepository.SaveChangesAsync(cancellationToken);

        return AttendanceResponseMapper.Map(attendanceRecord);
    }
}
