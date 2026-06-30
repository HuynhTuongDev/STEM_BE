<<<<<<< HEAD
using FluentValidation;
=======
using Google.Apis.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
>>>>>>> origin/develop
using STEM.Application.Dtos.Auth;
using STEM.Application.Interfaces;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Auth;

public class GoogleLoginHandler
{
<<<<<<< HEAD
    private const int TeacherRoleId = 3;

    private readonly IGoogleTokenVerifier _googleTokenVerifier;
    private readonly IUserRepository _userRepository;
    private readonly AuthSessionService _authSessionService;
    private readonly IValidator<GoogleLoginRequest> _validator;

    public GoogleLoginHandler(
        IGoogleTokenVerifier googleTokenVerifier,
        IUserRepository userRepository,
        AuthSessionService authSessionService,
        IValidator<GoogleLoginRequest> validator)
    {
        _googleTokenVerifier = googleTokenVerifier;
        _userRepository = userRepository;
        _authSessionService = authSessionService;
        _validator = validator;
    }

    public async Task<LoginResponse> Handle(GoogleLoginRequest request, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var googleUser = await _googleTokenVerifier.VerifyAsync(request.GetIdToken(), cancellationToken);
        if (!googleUser.EmailVerified)
        {
            throw new UnauthorizedAccessException("Google email is not verified.");
        }

        var user = await _userRepository.GetByEmailAsync(googleUser.Email, cancellationToken);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Teacher account not found.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Account is disabled.");
        }

        var isTeacher = user.RoleId == TeacherRoleId
            || string.Equals(user.Role?.Name, RoleNames.Teacher, StringComparison.OrdinalIgnoreCase);
        if (!isTeacher)
        {
            throw new UnauthorizedAccessException("Only teachers can login with Google.");
        }

        return await _authSessionService.CreateLoginSessionAsync(user, cancellationToken: cancellationToken);
=======
    private readonly IUserRepository _userRepository;
    private readonly ILoginHistoryRepository _loginHistoryRepository;
    private readonly IRepository<RefreshToken> _refreshTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public GoogleLoginHandler(
        IUserRepository userRepository,
        ILoginHistoryRepository loginHistoryRepository,
        IRepository<RefreshToken> refreshTokenRepository,
        ITokenService tokenService,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _loginHistoryRepository = loginHistoryRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    public async Task<GoogleLoginResponse> Handle(GoogleLoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken))
            throw new UnauthorizedAccessException("ID token is required.");

        var clientId = _configuration["GoogleSettings:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
            throw new UnauthorizedAccessException("Google ClientId is not configured.");

        GoogleJsonWebSignature.Payload? payload;
        try
        {
            var validationSettings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { clientId }
            };

            payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, validationSettings);
            
            var validIssuers = new[] { "https://accounts.google.com", "https://accounts.google.com/" };
            if (string.IsNullOrEmpty(payload.Issuer) || !validIssuers.Contains(payload.Issuer))
                throw new UnauthorizedAccessException("Invalid Google token issuer.");
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (InvalidOperationException)
        {
            throw new UnauthorizedAccessException("Invalid Google ID token.");
        }
        catch (Exception)
        {
            throw new UnauthorizedAccessException("Failed to validate Google ID token.");
        }

        if (string.IsNullOrEmpty(payload.Email))
            throw new UnauthorizedAccessException("Email not found in Google token.");

        var user = await _userRepository.GetByEmailAsync(payload.Email, cancellationToken);

        if (user == null)
            throw new UnauthorizedAccessException("Email chưa được đăng ký trong hệ thống. Vui lòng liên hệ nhà trường để được tạo tài khoản.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Tài khoản đã bị vô hiệu hóa.");

        user.FullName = string.IsNullOrWhiteSpace(user.FullName) 
            ? payload.Name ?? payload.Email 
            : user.FullName;
        user.Avatar = string.IsNullOrWhiteSpace(user.Avatar) ? payload.Picture : user.Avatar;
        
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        await RecordLoginHistoryAsync(user, cancellationToken);

        var token = _tokenService.GenerateAccessToken(user);
        var refreshToken = await CreateRefreshTokenAsync(user, cancellationToken);

        return new GoogleLoginResponse
        {
            Token = token,
            RefreshToken = refreshToken,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role?.Name ?? user.RoleId.ToString(),
            SchoolId = user.SchoolId
        };
    }

    private async Task RecordLoginHistoryAsync(User user, CancellationToken cancellationToken)
    {
        try
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext == null)
                return;

            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = httpContext.Request.Headers["User-Agent"].ToString() ?? "Unknown";
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
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to record login history: {ex.Message}");
        }
    }

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
>>>>>>> origin/develop
    }
}
