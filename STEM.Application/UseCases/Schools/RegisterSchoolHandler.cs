using BCrypt.Net;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using STEM.Application.Dtos.Schools;
using STEM.Application.Interfaces;
using STEM.Core.Entities.Schools;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Schools;

public class RegisterSchoolHandler
{
    private readonly IRepository<School> _schoolRepository;
    private readonly IUserRepository _userRepository;
    private readonly IValidator<SchoolRegistrationRequest> _validator;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public RegisterSchoolHandler(
        IRepository<School> schoolRepository,
        IUserRepository userRepository,
        IValidator<SchoolRegistrationRequest> validator,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _schoolRepository = schoolRepository;
        _userRepository = userRepository;
        _validator = validator;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task Handle(SchoolRegistrationRequest request, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var existingUser = await _userRepository.GetByEmailAsync(request.RepresentativeEmail, cancellationToken);
        if (existingUser != null)
            throw new InvalidOperationException("Email is already registered.");

        var verificationToken = Guid.NewGuid().ToString("N");
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
            StudentScale = request.StudentScale,
            RepresentativePosition = request.RepresentativePosition,
            Website = request.Website,
            Notes = request.Notes,
            AttachmentUrl = request.DocumentUrl,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _schoolRepository.AddAsync(school, cancellationToken);
        await _schoolRepository.SaveChangesAsync(cancellationToken);

        // Create User as School Admin (pending approval, email not verified)
        var user = new User
        {
            Email = request.RepresentativeEmail,
            FullName = request.RepresentativeName,
            Phone = request.Phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsActive = false,
            IsEmailVerified = false,
            VerificationToken = verificationToken,
            VerificationTokenExpires = now.AddHours(24),
            RoleId = 2, // School Administrator
            SchoolId = school.Id,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        // Send verification email
        var frontendBaseUrl = _configuration["AppSettings:FrontendBaseUrl"] ?? "https://localhost:3000";
        var verificationLink = $"{frontendBaseUrl}/verify-email?email={Uri.EscapeDataString(user.Email)}&token={verificationToken}";
        var body = $@"
            <h2>Welcome to STEM</h2>
            <p>Hello {user.FullName},</p>
            <p>Your school registration for <strong>{school.Name}</strong> has been received.</p>
            <p>Please verify your email by clicking the button below:</p>
            <p><a href='{verificationLink}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Verify Email</a></p>
            <p>This link will expire in 24 hours.</p>
            <p>After verification, your account will require Master Admin approval before you can log in.</p>
        ";
        await _emailService.SendEmailAsync(user.Email, "Verify your email for STEM", body, cancellationToken);
    }
}
