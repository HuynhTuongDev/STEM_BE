using STEM.Application.Dtos.Auth;

namespace STEM.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default);
    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
}
