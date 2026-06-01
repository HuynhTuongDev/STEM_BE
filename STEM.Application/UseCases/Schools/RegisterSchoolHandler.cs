using BCrypt.Net;
using FluentValidation;
using STEM.Application.Dtos.Schools;
using STEM.Core.Entities.Schools;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Schools;

public class RegisterSchoolHandler
{
    private readonly IRepository<School> _schoolRepository;
    private readonly IUserRepository _userRepository;
    private readonly IValidator<SchoolRegistrationRequest> _validator;

    public RegisterSchoolHandler(
        IRepository<School> schoolRepository,
        IUserRepository userRepository,
        IValidator<SchoolRegistrationRequest> validator)
    {
        _schoolRepository = schoolRepository;
        _userRepository = userRepository;
        _validator = validator;
    }

    public async Task Handle(SchoolRegistrationRequest request, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        // Check if email already registered
        var existingUser = await _userRepository.GetByEmailAsync(request.RepresentativeEmail, cancellationToken);
        if (existingUser != null)
            throw new InvalidOperationException("Email is already registered.");

        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

        // Create School (pending approval)
        var school = new School
        {
            Name = request.SchoolName,
            Address = request.SchoolAddress,
            RepresentativeName = request.RepresentativeName,
            RepresentativeEmail = request.RepresentativeEmail,
            ProofOfActivity = request.ProofOfActivity,
            Status = SchoolStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _schoolRepository.AddAsync(school, cancellationToken);
        await _schoolRepository.SaveChangesAsync(cancellationToken);

        // Create User as School Admin (pending approval)
        var user = new User
        {
            Email = request.RepresentativeEmail,
            FullName = request.RepresentativeName,
            Phone = request.Phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsActive = false, // Waiting for Master Admin approval
            IsEmailVerified = false,
            RoleId = 2, // School Administrator
            SchoolId = school.Id,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);
    }
}
