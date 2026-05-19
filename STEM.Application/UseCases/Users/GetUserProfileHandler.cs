using STEM.Application.Dtos.Users;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Users;

public class GetUserProfileHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IRepository<UserProfile> _userProfileRepository;

    public GetUserProfileHandler(IUserRepository userRepository, IRepository<UserProfile> userProfileRepository)
    {
        _userRepository = userRepository;
        _userProfileRepository = userProfileRepository;
    }

    public async Task<UserProfileDto> Handle(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            throw new KeyNotFoundException("User not found.");

        var profile = user.Profile ?? (await _userProfileRepository.FindAsync(p => p.UserId == userId, cancellationToken)).FirstOrDefault();

        return new UserProfileDto
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = profile?.FullName ?? string.Empty,
            Phone = profile?.Phone ?? string.Empty,
            Avatar = profile?.Avatar ?? string.Empty,
            Gender = profile?.Gender ?? string.Empty,
            DateOfBirth = profile?.DateOfBirth,
            Address = profile?.Address ?? string.Empty
        };
    }
}
