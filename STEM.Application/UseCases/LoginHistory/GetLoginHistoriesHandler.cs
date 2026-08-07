using STEM.Application.Dtos.LoginHistory;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.LoginHistory;

public class GetLoginHistoriesHandler
{
    private readonly ILoginHistoryRepository _loginHistoryRepository;
    private readonly IUserRepository _userRepository;

    public GetLoginHistoriesHandler(
        ILoginHistoryRepository loginHistoryRepository,
        IUserRepository userRepository)
    {
        _loginHistoryRepository = loginHistoryRepository;
        _userRepository = userRepository;
    }

    public async Task<List<LoginHistoryResponse>> Handle(GetLoginHistoriesRequest request, CancellationToken cancellationToken = default)
    {
        if (request.UserId <= 0)
            throw new ArgumentException("UserId is required.");

        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100);

        // Get target user to check SchoolId
        var targetUser = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (targetUser == null)
            throw new KeyNotFoundException("User not found.");

        // Get histories filtered by both UserId and SchoolId
        var histories = await _loginHistoryRepository.GetByUserIdAndSchoolIdAsync(
            request.UserId,
            targetUser.SchoolId ?? 0,
            cancellationToken);

        var pagedHistories = histories
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return pagedHistories.Select(h => new LoginHistoryResponse
        {
            Id = h.Id,
            UserId = h.UserId,
            LoginTime = h.CreatedAt,
            IpAddress = h.IpAddress ?? string.Empty,
            DeviceName = h.DeviceName ?? string.Empty,
            CreatedAt = h.CreatedAt
        }).ToList();
    }
}
