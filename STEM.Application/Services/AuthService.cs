using BCrypt.Net;
using STEM.Application.Interfaces;
using STEM.Core.DTOs.Auth;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtProvider _jwtProvider;
    private readonly IEmailService _emailService;

    public AuthService(IUserRepository userRepository, IJwtProvider jwtProvider, IEmailService emailService)
    {
        _userRepository = userRepository;
        _jwtProvider = jwtProvider;
        _emailService = emailService;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (!user.IsEmailVerified)
        {
            throw new UnauthorizedAccessException("Email is not verified.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Account is disabled.");
        }

        var token = _jwtProvider.GenerateToken(user);

        return new LoginResponse
        {
            Token = token,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.RoleId.ToString() // Replace with Role.Name if available
        };
    }

    public async Task RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
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
            VerificationTokenExpires = DateTime.UtcNow.AddDays(1),
            RoleId = 2 // Assuming 2 is a default role like "Student" or "User"
        };

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        // In a real application, construct a proper URL pointing to the frontend
        var verificationLink = $"https://yourfrontend.com/verify-email?email={user.Email}&token={verificationToken}";
        var body = $"Please verify your email by clicking <a href='{verificationLink}'>here</a>.";

        await _emailService.SendEmailAsync(user.Email, "Verify your email", body, cancellationToken);
    }

    public async Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException("Invalid email or token.");
        }

        if (user.IsEmailVerified)
        {
            throw new InvalidOperationException("Email is already verified.");
        }

        if (user.VerificationToken != request.Token || user.VerificationTokenExpires < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Invalid or expired verification token.");
        }

        user.IsEmailVerified = true;
        user.VerificationToken = null;
        user.VerificationTokenExpires = null;

        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null)
        {
            // Do not reveal that the user does not exist
            return;
        }

        var resetToken = Guid.NewGuid().ToString("N");
        user.ResetToken = resetToken;
        user.ResetTokenExpires = DateTime.UtcNow.AddHours(1);

        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        var resetLink = $"https://yourfrontend.com/reset-password?email={user.Email}&token={resetToken}";
        var body = $"You requested a password reset. Click <a href='{resetLink}'>here</a> to reset your password.";

        await _emailService.SendEmailAsync(user.Email, "Password Reset", body, cancellationToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException("Invalid email or token.");
        }

        if (user.ResetToken != request.Token || user.ResetTokenExpires < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Invalid or expired reset token.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.ResetToken = null;
        user.ResetTokenExpires = null;

        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);
    }
}
