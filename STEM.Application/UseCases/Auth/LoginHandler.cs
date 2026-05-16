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
    private readonly IRepository<RefreshToken> _refreshTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LoginHandler(
        IUserRepository userRepository,
        ILoginHistoryRepository loginHistoryRepository,
        IRepository<RefreshToken> refreshTokenRepository,
        ITokenService tokenService,
        IHttpContextAccessor httpContextAccessor)
    {
        _userRepository = userRepository;
        _loginHistoryRepository = loginHistoryRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
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

        var token = _tokenService.GenerateAccessToken(user);
        var refreshToken = await CreateRefreshTokenAsync(user, cancellationToken);

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
            RefreshToken = refreshToken,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role?.Name ?? user.RoleId.ToString()
        };
    }

    public string GenerateNewAccessToken(User user)
    {
        return _tokenService.GenerateAccessToken(user);
    }

    public async Task<LoginResponse> RefreshTokenAsync(string refreshTokenStr, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenStr))
            throw new UnauthorizedAccessException("Refresh token is required.");

        var refreshTokens = await _refreshTokenRepository.FindAsync(rt => rt.Token == refreshTokenStr, cancellationToken);
        var refreshToken = refreshTokens.FirstOrDefault();

        if (refreshToken == null)
            throw new UnauthorizedAccessException("Invalid refresh token.");

        var user = await _userRepository.GetByIdAsync(refreshToken.UserId, cancellationToken);
        if (user == null)
            throw new UnauthorizedAccessException("User not found.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is disabled.");

        // Generate new access token and refresh token
        // CreateRefreshTokenAsync will delete all old tokens and add new one
        var newAccessToken = _tokenService.GenerateAccessToken(user);
        var newRefreshToken = await CreateRefreshTokenAsync(user, cancellationToken);

        return new LoginResponse
        {
            Token = newAccessToken,
            RefreshToken = newRefreshToken,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role?.Name ?? user.RoleId.ToString()
        };
    }

    private async Task<string> CreateRefreshTokenAsync(User user, CancellationToken cancellationToken)
    {
        var existingTokens = await _refreshTokenRepository.FindAsync(rt => rt.UserId == user.Id, cancellationToken);
        foreach (var existingToken in existingTokens)
        {
            _refreshTokenRepository.Delete(existingToken);
        }

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = _tokenService.GenerateRefreshToken(),
            CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
        };

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        return refreshToken.Token;
    }
}
