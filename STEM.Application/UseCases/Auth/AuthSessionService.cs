using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using STEM.Application.Dtos.Auth;
using STEM.Application.Interfaces;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Auth;

public class AuthSessionService
{
    private readonly ILoginHistoryRepository _loginHistoryRepository;
    private readonly IRepository<RefreshToken> _refreshTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public AuthSessionService(
        ILoginHistoryRepository loginHistoryRepository,
        IRepository<RefreshToken> refreshTokenRepository,
        ITokenService tokenService,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration)
    {
        _loginHistoryRepository = loginHistoryRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    public async Task<LoginResponse> CreateLoginSessionAsync(
        User user,
        bool recordLoginHistory = true,
        CancellationToken cancellationToken = default)
    {
        var token = _tokenService.GenerateAccessToken(user);
        var refreshToken = await CreateRefreshTokenAsync(user, cancellationToken);

        if (recordLoginHistory)
        {
            await RecordLoginHistoryAsync(user.Id, cancellationToken);
        }

        return BuildLoginResponse(user, token, refreshToken);
    }

    private static LoginResponse BuildLoginResponse(User user, string accessToken, string refreshToken) =>
        new()
        {
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
        {
            _refreshTokenRepository.Delete(existingToken);
        }

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

    private async Task RecordLoginHistoryAsync(int userId, CancellationToken cancellationToken)
    {
        try
        {
            var ipAddress = _httpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = _httpContextAccessor?.HttpContext?.Request?.Headers["User-Agent"].ToString() ?? "Unknown";
            var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

            await _loginHistoryRepository.AddAsync(new LoginHistory
            {
                UserId = userId,
                IpAddress = ipAddress,
                DeviceName = userAgent,
                CreatedAt = now,
                UpdatedAt = now
            }, cancellationToken);
            await _loginHistoryRepository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to record login history: {ex.Message}");
        }
    }
}
