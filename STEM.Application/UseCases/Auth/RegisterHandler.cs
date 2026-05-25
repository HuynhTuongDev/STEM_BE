using BCrypt.Net;
using STEM.Application.Dtos.Auth;
using STEM.Application.Interfaces;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;
using FluentValidation;

namespace STEM.Application.UseCases.Auth;

public class RegisterHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly IValidator<RegisterRequest> _validator;

    public RegisterHandler(
        IUserRepository userRepository,
        IEmailService emailService,
        IValidator<RegisterRequest> validator)
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _validator = validator;
    }

    public async Task Handle(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);
        var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser != null)
            throw new InvalidOperationException("Email is already registered.");

        var verificationToken = Guid.NewGuid().ToString("N");
        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

        var user = new User
        {
            Email = request.Email,
            FullName = request.FullName,
            Phone = request.Phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsActive = true,
            IsEmailVerified = false,
            VerificationToken = verificationToken,
            VerificationTokenExpires = now.AddHours(1),
            RoleId = 4,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        var verificationLink = $"https://yourfrontend.com/verify-email?email={user.Email}&token={verificationToken}";
        var body = $"Please verify your email by clicking <a href='{verificationLink}'>here</a>.";
        await _emailService.SendEmailAsync(user.Email, "Verify your email", body, cancellationToken);
    }
}
