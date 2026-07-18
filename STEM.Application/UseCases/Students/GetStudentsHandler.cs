using STEM.Application.Dtos.Students;
using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Students;

public class GetStudentsHandler
{
    private const string StudentRoleName = "Student";

    private readonly IUserRepository _userRepository;
    private readonly IRepository<Role> _roleRepository;
    private readonly IRepository<Enrollment> _enrollmentRepository;

    public GetStudentsHandler(
        IUserRepository userRepository,
        IRepository<Role> roleRepository,
        IRepository<Enrollment> enrollmentRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _enrollmentRepository = enrollmentRepository;
    }

    public async Task<StudentsListResponse> Handle(GetStudentsRequest request, int currentUserId, CancellationToken cancellationToken = default)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
        {
            throw new UnauthorizedAccessException("Current user not found.");
        }

        if (currentUser.SchoolId == null)
        {
            throw new UnauthorizedAccessException("School administrator must belong to a school.");
        }
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100);

        var studentRole = (await _roleRepository.FindAsync(role => role.Name == StudentRoleName, cancellationToken))
            .FirstOrDefault();

        if (studentRole == null)
        {
            return new StudentsListResponse
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        var students = (await _userRepository.FindAsync(user => user.RoleId == studentRole.Id && user.SchoolId == currentUser.SchoolId, cancellationToken))
            .ToList();

        if (request.IsActive.HasValue)
        {
            students = students.Where(student => student.IsActive == request.IsActive.Value).ToList();
        }

        var search = request.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            students = students
                .Where(student =>
                    student.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    student.Email.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var total = students.Count;
        var pagedStudents = students
            .OrderBy(student => student.FullName)
            .ThenBy(student => student.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var studentIds = pagedStudents.Select(student => student.Id).ToList();
        var enrollments = studentIds.Count == 0
            ? new List<Enrollment>()
            : (await _enrollmentRepository.FindAsync(enrollment => studentIds.Contains(enrollment.StudentId), cancellationToken)).ToList();

        var enrollmentCounts = enrollments
            .GroupBy(enrollment => enrollment.StudentId)
            .ToDictionary(group => group.Key, group => group.Select(enrollment => enrollment.ClassId).Distinct().Count());

        return new StudentsListResponse
        {
            Total = total,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Data = pagedStudents.Select(student => new StudentResponse
            {
                Id = student.Id,
                FullName = student.FullName,
                Email = student.Email,
                Phone = student.Phone,
                Avatar = student.Avatar,
                Gender = student.Gender,
                DateOfBirth = student.DateOfBirth,
                Address = student.Address,
                IsActive = student.IsActive,
                IsEmailVerified = student.IsEmailVerified,
                SchoolId = student.SchoolId,
                SchoolName = student.School?.Name,
                TotalEnrolledClasses = enrollmentCounts.GetValueOrDefault(student.Id),
                CertificatesEarned = 0,
                AverageScore = null,
                CreatedAt = student.CreatedAt,
                UpdatedAt = student.UpdatedAt
            }).ToList()
        };
    }
}
