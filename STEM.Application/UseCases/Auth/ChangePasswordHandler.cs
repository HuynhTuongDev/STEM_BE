using BCrypt.Net;
using STEM.Application.Dtos.Auth;
using STEM.Core.Repository;
using FluentValidation;

namespace STEM.Application.UseCases.Auth;

public class ChangePasswordHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IValidator<ChangePasswordRequest> _validator;

    public ChangePasswordHandler(IUserRepository userRepository, IValidator<ChangePasswordRequest> validator)
    {
        _userRepository = userRepository;
        _validator = validator;
    }

    public async Task Handle(int userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            throw new KeyNotFoundException("User not found.");

        if (user.RoleId != 1 && user.RoleId != 2)
            throw new UnauthorizedAccessException("Password change is only available for Master Admin or School Administrators.");

        if (string.IsNullOrWhiteSpace(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Incorrect old password.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

        await _userRepository.SaveChangesAsync(cancellationToken);
    }
}
