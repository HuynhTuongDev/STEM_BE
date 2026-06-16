using STEM.Application.Dtos.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Users;

public class GetUserDetailHandler
{
    private readonly IUserRepository _userRepository;

    public GetUserDetailHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDetailResponse> Handle(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
            throw new KeyNotFoundException($"User with id {userId} not found.");

        return new UserDetailResponse
        {
            UserId           = user.Id,
            Email            = user.Email,
            FullName         = user.FullName,
            Phone            = user.Phone,
            Avatar           = user.Avatar,
            Gender           = user.Gender,
            DateOfBirth      = user.DateOfBirth,
            Address          = user.Address,
            IsActive         = user.IsActive,
            IsEmailVerified  = user.IsEmailVerified,
            RoleId           = user.RoleId,
            Role             = user.Role?.Name ?? string.Empty,
            SchoolId         = user.SchoolId,
            CreatedAt        = user.CreatedAt,
            UpdatedAt        = user.UpdatedAt
        };
    }
}
