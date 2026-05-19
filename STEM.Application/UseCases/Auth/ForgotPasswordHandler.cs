using STEM.Application.Dtos.Auth;
using STEM.Application.Interfaces;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Auth;

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
        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

        user.UpdatedAt = now;
        user.ResetToken = resetToken;
        user.ResetTokenExpires = now.AddHours(1);

        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        try
        {
            var resetLink = $"https://yourfrontend.com/reset-password?email={user.Email}&token={resetToken}";
            var body = $"Reset your password by clicking <a href='{resetLink}'>here</a>.";
            await _emailService.SendEmailAsync(user.Email, "Reset Password", body, cancellationToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to send reset email: {ex.Message}");
        }
    }
}
