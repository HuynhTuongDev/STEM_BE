using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using STEM.Application.Interfaces;

namespace STEM.Infrastructure.Services.Authentication;

public class GoogleTokenVerifier : IGoogleTokenVerifier
{
    private readonly IConfiguration _configuration;

    public GoogleTokenVerifier(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<GoogleUserInfo> VerifyAsync(string idToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            throw new UnauthorizedAccessException("Google id token is required.");
        }

        var clientIds = GetConfiguredClientIds();
        if (clientIds.Count == 0)
        {
            throw new InvalidOperationException("Google ClientId is not configured.");
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var payload = await GoogleJsonWebSignature.ValidateAsync(
                idToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = clientIds
                });

            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(payload.Email))
            {
                throw new UnauthorizedAccessException("Google token does not contain an email.");
            }

            return new GoogleUserInfo
            {
                Email = payload.Email.Trim(),
                Name = payload.Name ?? string.Empty,
                Picture = payload.Picture,
                EmailVerified = payload.EmailVerified
            };
        }
        catch (InvalidJwtException ex)
        {
            throw new UnauthorizedAccessException("Invalid Google token.", ex);
        }
    }

    private IReadOnlyList<string> GetConfiguredClientIds()
    {
        var clientIds = new List<string>();
        AddClientId(clientIds, _configuration["Authentication:Google:ClientId"]);
        AddClientId(clientIds, _configuration["GoogleAuth:ClientId"]);

        foreach (var configuredClientId in _configuration.GetSection("Authentication:Google:ClientIds").GetChildren())
        {
            AddClientId(clientIds, configuredClientId.Value);
        }

        return clientIds
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static void AddClientId(ICollection<string> clientIds, string? clientId)
    {
        if (!string.IsNullOrWhiteSpace(clientId))
        {
            clientIds.Add(clientId.Trim());
        }
    }
}
