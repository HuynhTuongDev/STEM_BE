using BCrypt.Net;
using STEM.Application.Dtos.Auth;
using STEM.Application.Interfaces;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Auth;

/// <summary>
/// Handler for user registration use case
/// </summary>
public class RegisterHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;

    public RegisterHandler(IUserRepository userRepository, IEmailService emailService)
    {
        _userRepository = userRepository;
        _emailService = emailService;
    }

    public async Task Handle(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser != null)
        {
            throw new InvalidOperationException("Email is already registered.");
        }

        var verificationToken = Guid.NewGuid().ToString("N");

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsActive = true,
            IsEmailVerified = false,
            VerificationToken = verificationToken,
            VerificationTokenExpires = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(1), DateTimeKind.Utc),
            RoleId = 2,
            CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
        };

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        var verificationLink = $"https://yourfrontend.com/verify-email?email={user.Email}&token={verificationToken}";
        var body = $"Please verify your email by clicking <a href='{verificationLink}'>here</a>.";

        await _emailService.SendEmailAsync(user.Email, "Verify your email", body, cancellationToken);
    }
}
