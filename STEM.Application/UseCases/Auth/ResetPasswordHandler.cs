using STEM.Application.Dtos.Auth;
using STEM.Application.Interfaces;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Auth;

/// <summary>
/// Handler for forgot password use case
/// </summary>
public class ForgotPasswordHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;

    public ForgotPasswordHandler(IUserRepository userRepository, IEmailService emailService)
    {
        _userRepository = userRepository;
        _emailService = emailService;
    }

    public async Task Handle(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null) return;

        var resetToken = Guid.NewGuid().ToString("N");
        user.ResetToken = resetToken;
        user.ResetTokenExpires = DateTime.SpecifyKind(DateTime.UtcNow.AddHours(1), DateTimeKind.Utc);
        user.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        var resetLink = $"https://yourfrontend.com/reset-password?email={user.Email}&token={resetToken}";
        var body = $"Reset your password by clicking <a href='{resetLink}'>here</a>.";

        await _emailService.SendEmailAsync(user.Email, "Reset Password", body, cancellationToken);
    }
}
