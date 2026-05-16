using STEM.Core.Entities.Users;

namespace STEM.Application.Interfaces;

/// <summary>
/// Interface for token generation (JWT access tokens and refresh tokens)
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generate JWT access token for user
    /// </summary>
    string GenerateAccessToken(User user);

    /// <summary>
    /// Generate secure refresh token
    /// </summary>
    string GenerateRefreshToken();
}
