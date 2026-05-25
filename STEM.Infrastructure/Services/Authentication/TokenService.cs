using System.Security.Cryptography;
using STEM.Application.Interfaces;
using STEM.Core.Entities.Users;

namespace STEM.Infrastructure.Services.Authentication;

/// <summary>
/// Service to generate both JWT access tokens and refresh tokens
/// </summary>
public class TokenService : ITokenService
{
    private readonly IJwtProvider _jwtProvider;

    public TokenService(IJwtProvider jwtProvider)
    {
        _jwtProvider = jwtProvider;
    }

    /// <summary>
    /// Generate JWT access token for user
    /// </summary>
    public string GenerateAccessToken(User user)
    {
        var displayName = string.IsNullOrWhiteSpace(user.FullName) ? user.Email : user.FullName;
        return _jwtProvider.GenerateToken(user, displayName);
    }

    /// <summary>
    /// Generate secure refresh token (random 64 bytes encoded in Base64)
    /// </summary>
    public string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }
}
