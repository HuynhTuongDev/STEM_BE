using STEM.Application.Dtos.Students;
using STEM.Core.Entities.Schools;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Students;

public class CreateStudentHandler
{
    private const string StudentRoleName = "Student";

    private readonly IUserRepository _userRepository;
    private readonly IRepository<Role> _roleRepository;
    private readonly IRepository<School> _schoolRepository;

    public CreateStudentHandler(
        IUserRepository userRepository,
        IRepository<Role> roleRepository,
        IRepository<School> schoolRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _schoolRepository = schoolRepository;
    }

    public async Task<StudentResponse> Handle(CreateStudentRequest request, int currentUserId, CancellationToken cancellationToken = default)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null || currentUser.SchoolId == null)
        {
            throw new InvalidOperationException("Current user must belong to a school.");
        }

        var studentRole = (await _roleRepository.FindAsync(r => r.Name == StudentRoleName, cancellationToken))
            .FirstOrDefault();

        if (studentRole == null)
        {
            throw new InvalidOperationException("Student role not found in the system.");
        }

        var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser != null)
        {
            throw new InvalidOperationException("A user with this email already exists.");
        }

        var student = new User
        {
            Email = request.Email.Trim().ToLower(),
            FullName = request.FullName.Trim(),
            Phone = request.Phone?.Trim(),
            Gender = request.Gender?.Trim(),
            DateOfBirth = request.DateOfBirth,
            Address = request.Address?.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password ?? "TempPassword123"),
            RoleId = studentRole.Id,
            SchoolId = currentUser.SchoolId,
            IsActive = request.IsActive,
            IsEmailVerified = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(student, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return new StudentResponse
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
            TotalEnrolledClasses = 0,
            CertificatesEarned = 0,
            AverageScore = null,
            CreatedAt = student.CreatedAt,
            UpdatedAt = student.UpdatedAt
        };
    }
}
