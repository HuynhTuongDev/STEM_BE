using STEM.Application.Dtos.Auth;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Auth;

/// <summary>
/// Handler for email verification use case
/// </summary>
public class VerifyEmailHandler
{
    private readonly IUserRepository _userRepository;

    public VerifyEmailHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task Handle(VerifyEmailRequest request, CancellationToken cancellationToken = default)
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
        user.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);
    }
}
