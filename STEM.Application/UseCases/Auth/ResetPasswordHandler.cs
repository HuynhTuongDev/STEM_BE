using BCrypt.Net;
using STEM.Application.Dtos.Auth;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Auth;

public class ResetPasswordHandler
{
    private readonly IUserRepository _userRepository;

    public ResetPasswordHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task Handle(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Token) ||
            string.IsNullOrWhiteSpace(request.NewPassword))
            throw new InvalidOperationException("Email, token, and new password are required.");

        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null)
            throw new InvalidOperationException("User not found.");

        if (user.ResetToken != request.Token)
            throw new InvalidOperationException("Invalid reset token.");

        if (user.ResetTokenExpires == null || user.ResetTokenExpires < DateTime.UtcNow)
            throw new InvalidOperationException("Reset token has expired.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.ResetToken = null;
        user.ResetTokenExpires = null;
        user.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);
    }
}
