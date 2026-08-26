using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using STEM.Application.Dtos.Auth;
using STEM.Application.Interfaces;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;
using FluentValidation;
using BCrypt.Net;
using STEM.Core.Entities.Schools;

namespace STEM.Application.UseCases.Auth;

public class LoginHandler
{
    private readonly IUserRepository _userRepository;
    private readonly ILoginHistoryRepository _loginHistoryRepository;
    private readonly IRepository<RefreshToken> _refreshTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly IValidator<LoginRequest> _validator;

    public LoginHandler(
        IUserRepository userRepository,
        ILoginHistoryRepository loginHistoryRepository,
        IRepository<RefreshToken> refreshTokenRepository,
        ITokenService tokenService,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        IValidator<LoginRequest> validator)
    {
        _userRepository = userRepository;
        _loginHistoryRepository = loginHistoryRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _validator = validator;
    }

    public async Task<LoginResponse> Handle(LoginRequest request, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null)
            throw new UnauthorizedAccessException("User not found.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is disabled.");

        // Check if the user's school is locked (for school-related users)
        if (user.SchoolId.HasValue && user.School != null && user.School.Status == SchoolStatus.Rejected)
        {
            throw new UnauthorizedAccessException("Trường học của bạn đang bị khóa. Vui lòng liên hệ quản trị viên hệ thống.");
        }

        // Verify password
        if (string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        if (user.RoleId == 3 || user.RoleId == 4) // Student/Teacher - must use Google OAuth
        {
            throw new UnauthorizedAccessException("Vui lòng đăng nhập bằng Google OAuth.");
        }

        var token = _tokenService.GenerateAccessToken(user);
        var refreshToken = await CreateRefreshTokenAsync(user, cancellationToken);

        try
        {
            var ipAddress = _httpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = _httpContextAccessor?.HttpContext?.Request?.Headers["User-Agent"].ToString() ?? "Unknown";
            var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

            await _loginHistoryRepository.AddAsync(new STEM.Core.Entities.Users.LoginHistory
            {
                UserId = user.Id,
                IpAddress = ipAddress,
                DeviceName = userAgent,
                CreatedAt = now,
                UpdatedAt = now
            }, cancellationToken);
            await _loginHistoryRepository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception)
        {
            // Silently ignore login history recording failures
        }

        return BuildLoginResponse(user, token, refreshToken);
    }

    public async Task<LoginResponse> RefreshTokenAsync(string refreshTokenStr, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenStr))
            throw new UnauthorizedAccessException("Refresh token is required.");

        var refreshTokens = await _refreshTokenRepository.FindAsync(
            rt => rt.Token == refreshTokenStr,
            cancellationToken);
        var refreshToken = refreshTokens.FirstOrDefault();

        if (refreshToken == null)
            throw new UnauthorizedAccessException("Invalid refresh token.");

        if (refreshToken.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token has expired.");

        var user = await _userRepository.GetByIdAsync(refreshToken.UserId, cancellationToken);
        if (user == null)
            throw new UnauthorizedAccessException("User not found.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is disabled.");

        // Check if the user's school is locked (for school-related users)
        if (user.SchoolId.HasValue && user.School != null && user.School.Status == SchoolStatus.Rejected)
        {
            throw new UnauthorizedAccessException("Trường học của bạn đang bị khóa. Vui lòng liên hệ quản trị viên hệ thống.");
        }

        var newAccessToken = _tokenService.GenerateAccessToken(user);
        var newRefreshToken = await CreateRefreshTokenAsync(user, cancellationToken);

        return BuildLoginResponse(user, newAccessToken, newRefreshToken);
    }

    private static LoginResponse BuildLoginResponse(User user, string accessToken, string refreshToken) =>
        new()
        {
            Id = user.Id,
            Token = accessToken,
            RefreshToken = refreshToken,
            Email = user.Email,
            FullName = string.IsNullOrWhiteSpace(user.FullName) ? user.Email : user.FullName,
            Role = user.Role?.Name ?? user.RoleId.ToString()
        };

    private async Task<string> CreateRefreshTokenAsync(User user, CancellationToken cancellationToken)
    {
        var existingTokens = await _refreshTokenRepository.FindAsync(
            rt => rt.UserId == user.Id,
            cancellationToken);
        foreach (var existingToken in existingTokens)
            _refreshTokenRepository.Delete(existingToken);

        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
        var expirationDays = int.Parse(_configuration["JwtSettings:RefreshTokenExpirationInDays"] ?? "7");

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = _tokenService.GenerateRefreshToken(),
            ExpiresAt = now.AddDays(expirationDays),
            CreatedAt = now,
            UpdatedAt = now
        };

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        return refreshToken.Token;
    }
}
