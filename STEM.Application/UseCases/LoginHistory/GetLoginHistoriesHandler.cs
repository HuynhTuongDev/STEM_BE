using STEM.Application.Dtos.LoginHistory;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.LoginHistory;

public class GetLoginHistoriesHandler
{
    private readonly ILoginHistoryRepository _loginHistoryRepository;

    public GetLoginHistoriesHandler(ILoginHistoryRepository loginHistoryRepository)
    {
        _loginHistoryRepository = loginHistoryRepository;
    }

    public async Task<List<LoginHistoryResponse>> Handle(GetLoginHistoriesRequest request, CancellationToken cancellationToken = default)
    {
        var histories = await _loginHistoryRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        var pagedHistories = histories
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return pagedHistories.Select(h => new LoginHistoryResponse
        {
            Id = h.Id,
            UserId = h.UserId,
            LoginTime = h.LoginTime,
            IpAddress = h.IpAddress ?? string.Empty,
            DeviceName = h.DeviceName ?? string.Empty,
            CreatedAt = h.CreatedAt
        }).ToList();
    }
}
