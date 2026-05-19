using STEM.Application.Dtos.Users;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;
using FluentValidation;

namespace STEM.Application.UseCases.Users;

public class UpdateUserProfileHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IRepository<UserProfile> _userProfileRepository;
    private readonly IValidator<UpdateProfileRequest> _validator;

    public UpdateUserProfileHandler(
        IUserRepository userRepository,
        IRepository<UserProfile> userProfileRepository,
        IValidator<UpdateProfileRequest> validator)
    {
        _userRepository = userRepository;
        _userProfileRepository = userProfileRepository;
        _validator = validator;
    }

    public async Task<UserProfileDto> Handle(int userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            throw new KeyNotFoundException("User not found.");

        var profile = user.Profile
            ?? (await _userProfileRepository.FindAsync(p => p.UserId == userId, cancellationToken)).FirstOrDefault();

        var now = DateTime.UtcNow;

        if (profile == null)
        {
            profile = new UserProfile
            {
                UserId = userId,
                FullName = request.FullName,
                Phone = request.Phone,
                Gender = request.Gender,
                DateOfBirth = request.DateOfBirth,
                Address = request.Address,
                CreatedAt = now,
                UpdatedAt = now
            };
            await _userProfileRepository.AddAsync(profile, cancellationToken);
        }
        else
        {
            profile.FullName = request.FullName;
            profile.Phone = request.Phone;
            profile.Gender = request.Gender;
            profile.DateOfBirth = request.DateOfBirth;
            profile.Address = request.Address;
            profile.UpdatedAt = now;
            _userProfileRepository.Update(profile);
        }

        user.UpdatedAt = now;
        _userRepository.Update(user);
        await _userProfileRepository.SaveChangesAsync(cancellationToken);

        return new UserProfileDto
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = profile.FullName,
            Phone = profile.Phone,
            Avatar = profile.Avatar,
            Gender = profile.Gender ?? string.Empty,
            DateOfBirth = profile.DateOfBirth,
            Address = profile.Address ?? string.Empty
        };
    }
}
