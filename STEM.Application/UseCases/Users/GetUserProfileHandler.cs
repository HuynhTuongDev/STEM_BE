using STEM.Application.Dtos.Users;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Users;

public class GetUserProfileHandler
{
    private readonly IUserRepository _userRepository;

    public GetUserProfileHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserProfileResponse> Handle(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            throw new KeyNotFoundException("User not found.");

        return new UserProfileResponse
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Phone = user.Phone ?? string.Empty,
            Avatar = user.Avatar ?? string.Empty,
            Gender = user.Gender ?? string.Empty,
            DateOfBirth = user.DateOfBirth,
            Address = user.Address ?? string.Empty,
            CreatedAt = user.CreatedAt
        };
    }
}
