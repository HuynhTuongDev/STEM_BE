using STEM.Application.Dtos.Users;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;
using FluentValidation;

namespace STEM.Application.UseCases.Users;

public class UpdateUserProfileHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IValidator<UpdateProfileRequest> _validator;

    public UpdateUserProfileHandler(
        IUserRepository userRepository,
        IValidator<UpdateProfileRequest> validator)
    {
        _userRepository = userRepository;
        _validator = validator;
    }

    public async Task<UserProfileDto> Handle(int userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            throw new KeyNotFoundException("User not found.");

        var now = DateTime.UtcNow;

        user.FullName = request.FullName;
        user.Phone = request.Phone;
        user.Gender = request.Gender;
        user.DateOfBirth = request.DateOfBirth;
        user.Address = request.Address;
        user.UpdatedAt = now;

        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return new UserProfileDto
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Phone = user.Phone ?? string.Empty,
            Avatar = user.Avatar ?? string.Empty,
            Gender = user.Gender ?? string.Empty,
            DateOfBirth = user.DateOfBirth,
            Address = user.Address ?? string.Empty
        };
    }
}
