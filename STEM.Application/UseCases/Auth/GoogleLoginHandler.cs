using FluentValidation;
using STEM.Application.Dtos.Auth;
using STEM.Application.Interfaces;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Auth;

public class GoogleLoginHandler
{
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
    }
}
