using Microsoft.Extensions.Configuration;
using STEM.Application.Dtos.Auth;
using STEM.Application.Interfaces;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Auth;

public class ForgotPasswordHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly string _frontendBaseUrl;

    public ForgotPasswordHandler(
        IUserRepository userRepository,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _frontendBaseUrl = configuration["AppSettings:FrontendBaseUrl"] ?? "http://localhost:5173";
    }

    public async Task Handle(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null)
            throw new InvalidOperationException("If the email is registered, a password reset link has been sent.");

        if (user.RoleId != 2) // Only School Administrator can reset password
            throw new UnauthorizedAccessException("Password reset is only available for School Administrators.");

        var resetToken = Guid.NewGuid().ToString("N");
        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

        user.UpdatedAt = now;
        user.ResetToken = resetToken;
        user.ResetTokenExpires = now.AddHours(1);

        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        try
        {
            var resetLink = $"{_frontendBaseUrl}/reset-password?email={Uri.EscapeDataString(user.Email)}&token={resetToken}";
            var body = $"Reset your password by clicking <a href='{resetLink}'>here</a>.";
            await _emailService.SendEmailAsync(user.Email, "Reset Password", body, cancellationToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to send reset email: {ex.Message}");
        }
    }
}
