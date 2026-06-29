using STEM.Application.Dtos.Attendance;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Attendance;

public class GetAttendanceHandler
{
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IUserRepository _userRepository;

    public GetAttendanceHandler(
        IAttendanceRepository attendanceRepository,
        IUserRepository userRepository)
    {
        _attendanceRepository = attendanceRepository;
        _userRepository = userRepository;
    }

    public async Task<PagedAttendanceResponse> Handle(
        GetAttendanceRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
        {
            throw new UnauthorizedAccessException("User does not exist.");
        }

        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100);

        int? schoolId = null;
        int? teacherId = null;
        var studentId = request.StudentId;
        var roleName = currentUser.Role?.Name;

        if (roleName == RoleNames.SchoolAdministrator)
        {
            schoolId = currentUser.SchoolId ?? throw new UnauthorizedAccessException("School admin has no school.");
        }
        else if (roleName == RoleNames.Teacher)
        {
            teacherId = currentUser.Id;
        }
        else if (roleName == RoleNames.Student)
        {
            if (request.StudentId.HasValue && request.StudentId.Value != currentUser.Id)
            {
                throw new UnauthorizedAccessException("Students can only view their own attendance.");
            }

            studentId = currentUser.Id;
        }
        else
        {
            throw new UnauthorizedAccessException("You are not allowed to view attendance.");
        }

        var (records, totalCount) = await _attendanceRepository.GetPagedAsync(
            pageNumber,
            pageSize,
            request.ClassId,
            studentId,
            request.AttendanceDate,
            schoolId,
            teacherId,
            cancellationToken);

        return new PagedAttendanceResponse
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = records.Select(AttendanceResponseMapper.Map).ToList()
        };
    }
}
