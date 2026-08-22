using STEM.Application.Dtos.SystemLogs;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.SystemLogs;

public class GetSystemLogsListHandler
{
    private readonly ISystemLogRepository _systemLogRepository;
    private readonly IUserRepository _userRepository;

    public GetSystemLogsListHandler(ISystemLogRepository systemLogRepository, IUserRepository userRepository)
    {
        _systemLogRepository = systemLogRepository;
        _userRepository = userRepository;
    }

    public async Task<PagedSystemLogListResponse> Handle(
        GetSystemLogsRequest request,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100);

        var (logs, totalCount) = await _systemLogRepository.GetPagedAsync(
            pageNumber,
            pageSize,
            request.Action,
            request.Level,
            request.ActorUserId,
            request.EntityType,
            request.EntityId,
            request.From,
            request.To,
            cancellationToken);

        var logsList = logs.ToList();

        var actorIds = logsList
            .Where(l => l.ActorUserId.HasValue)
            .Select(l => l.ActorUserId!.Value)
            .Distinct()
            .ToList();

        var actorNames = new Dictionary<int, string>();
        foreach (var actorId in actorIds)
        {
            var actor = await _userRepository.GetByIdAsync(actorId, cancellationToken);
            if (actor != null)
                actorNames[actorId] = actor.FullName;
        }

        var items = logsList.Select(l => new SystemLogItemResponse
        {
            Id = l.Id,
            Level = l.Level,
            Action = l.Action,
            ActorUserId = l.ActorUserId,
            ActorName = l.ActorUserId.HasValue && actorNames.TryGetValue(l.ActorUserId.Value, out var name) ? name : null,
            ActorRole = l.ActorRole,
            EntityType = l.EntityType,
            EntityId = l.EntityId,
            Description = l.Description,
            Metadata = l.MetadataJson,
            CreatedAt = l.CreatedAt
        }).ToList();

        return new PagedSystemLogListResponse
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = items
        };
    }
}
