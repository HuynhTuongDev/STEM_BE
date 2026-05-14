using BCrypt.Net;
using Microsoft.AspNetCore.Http;
using STEM.Application.Dtos.Auth;
using STEM.Application.Interfaces;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Auth;

/// <summary>
/// Handler for user login use case
/// </summary>
public class LoginHandler
{
    private readonly IUserRepository _userRepository;
    private readonly ILoginHistoryRepository _loginHistoryRepository;
    private readonly IJwtProvider _jwtProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LoginHandler(
        IUserRepository userRepository,
        ILoginHistoryRepository loginHistoryRepository,
        IJwtProvider jwtProvider,
        IHttpContextAccessor httpContextAccessor)
    {
        _userRepository = userRepository;
        _loginHistoryRepository = loginHistoryRepository;
        _jwtProvider = jwtProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<LoginResponse> Handle(LoginRequest request, CancellationToken cancellationToken = default)
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

        // Record login history
        try
        {
            var ipAddress = _httpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = _httpContextAccessor?.HttpContext?.Request?.Headers["User-Agent"].ToString() ?? "Unknown";

            var loginHistory = new STEM.Core.Entities.Users.LoginHistory
            {
                UserId = user.Id,
                LoginTime = DateTime.UtcNow,
                IpAddress = ipAddress,
                DeviceName = userAgent
            };

            await _loginHistoryRepository.AddAsync(loginHistory, cancellationToken);
            await _loginHistoryRepository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Log the exception but don't fail the login process
            Console.WriteLine($"Failed to record login history: {ex.Message}");
        }

        return new LoginResponse
        {
            Token = token,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.RoleId.ToString()
        };
    }
}
