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

    public UpdateUserProfileHandler(IUserRepository userRepository, IRepository<UserProfile> userProfileRepository, IValidator<UpdateProfileRequest> validator)
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

        // Update basic user info
        user.FullName = request.FullName;
        user.Phone = request.Phone;
        user.UpdatedAt = DateTime.UtcNow;
        _userRepository.Update(user);

        // Update or create profile info
        var profiles = await _userProfileRepository.FindAsync(p => p.UserId == userId, cancellationToken);
        var profile = profiles.FirstOrDefault();

        if (profile == null)
        {
            profile = new UserProfile
            {
                UserId = userId,
                Gender = request.Gender,
                DateOfBirth = request.DateOfBirth ?? default,
                Address = request.Address,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _userProfileRepository.AddAsync(profile, cancellationToken);
        }
        else
        {
            profile.Gender = request.Gender;
            profile.DateOfBirth = request.DateOfBirth ?? default;
            profile.Address = request.Address;
            profile.UpdatedAt = DateTime.UtcNow;
            _userProfileRepository.Update(profile);
        }

        await _userRepository.SaveChangesAsync(cancellationToken); // Save both user and profile changes

        return new UserProfileDto
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Phone = user.Phone,
            Avatar = user.Avatar,
            Gender = profile.Gender,
            DateOfBirth = profile.DateOfBirth,
            Address = profile.Address
        };
    }
}
