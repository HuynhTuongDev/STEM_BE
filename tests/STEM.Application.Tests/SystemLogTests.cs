using STEM.Application.Dtos.SystemLogs;
using STEM.Application.UseCases.SystemLogs;
using STEM.Core.Entities.Common;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.Tests;

internal class FakeSystemLogRepository : ISystemLogRepository
{
    public readonly List<SystemLog> Items = new();

    public Task AddAsync(SystemLog log, CancellationToken cancellationToken = default)
    {
        if (log.Id == 0)
            log.Id = Items.Count == 0 ? 1 : Items.Max(i => i.Id) + 1;
        Items.Add(log);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<SystemLog?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(l => l.Id == id));

    public Task<(IEnumerable<SystemLog> Logs, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize, string? action, string? level, int? actorUserId,
        string? entityType, string? entityId, DateTime? from, DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var query = Items.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(l => l.Action == action);
        if (!string.IsNullOrWhiteSpace(level)) query = query.Where(l => l.Level == level);
        if (actorUserId.HasValue) query = query.Where(l => l.ActorUserId == actorUserId.Value);
        if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(l => l.EntityType == entityType);
        if (!string.IsNullOrWhiteSpace(entityId)) query = query.Where(l => l.EntityId == entityId);
        if (from.HasValue) query = query.Where(l => l.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(l => l.CreatedAt <= to.Value);

        var ordered = query.OrderByDescending(l => l.CreatedAt).ToList();
        var page = ordered.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult<(IEnumerable<SystemLog>, int)>((page, ordered.Count));
    }
}

public class SystemLogTests
{
    // ---- STEP 26: entity/service-level behavior ----

    [Fact]
    public void SystemLog_DoesNotInheritBaseEntity_HasNoUpdatedAt()
    {
        // Immutability by construction: there is no UpdatedAt property to set.
        var type = typeof(SystemLog);
        Assert.Null(type.GetProperty("UpdatedAt"));
        Assert.NotNull(type.GetProperty("CreatedAt"));
    }

    [Fact]
    public async Task SystemLogRepository_Add_AssignsCreatedAt_AndNullableActor()
    {
        var repo = new FakeSystemLogRepository();
        var log = new SystemLog
        {
            Level = SystemLogLevels.Information,
            Action = SystemLogActions.SyllabusCreated,
            ActorUserId = null, // system/unattributed event must be representable
            Description = "test",
            CreatedAt = DateTime.UtcNow
        };

        await repo.AddAsync(log);
        await repo.SaveChangesAsync();

        Assert.Single(repo.Items);
        Assert.Null(repo.Items[0].ActorUserId);
        Assert.True(repo.Items[0].CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void SystemLogLevels_RejectsUnknownLevel_ViaAllowList()
    {
        Assert.Contains(SystemLogLevels.Warning, SystemLogLevels.All);
        Assert.DoesNotContain("Debug", SystemLogLevels.All); // not a business-audit level
    }

    // ---- STEP 29: query filtering/sorting ----

    private static SystemLog MakeLog(string action, string level, int? actorUserId, DateTime createdAt, string? entityType = null, string? entityId = null) =>
        new() { Action = action, Level = level, ActorUserId = actorUserId, Description = "x", CreatedAt = createdAt, EntityType = entityType, EntityId = entityId };

    [Fact]
    public async Task GetSystemLogs_ReturnsNewestFirst()
    {
        var repo = new FakeSystemLogRepository();
        await repo.AddAsync(MakeLog(SystemLogActions.SyllabusCreated, SystemLogLevels.Information, 1, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        await repo.AddAsync(MakeLog(SystemLogActions.SyllabusUpdated, SystemLogLevels.Information, 1, new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc)));
        await repo.AddAsync(MakeLog(SystemLogActions.SyllabusArchived, SystemLogLevels.Warning, 1, new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)));

        var handler = new GetSystemLogsListHandler(repo, new FakeUserRepository());
        var result = await handler.Handle(new GetSystemLogsRequest());

        Assert.Equal(new[] { SystemLogActions.SyllabusUpdated, SystemLogActions.SyllabusArchived, SystemLogActions.SyllabusCreated },
            result.Items.Select(i => i.Action));
    }

    [Fact]
    public async Task GetSystemLogs_FiltersByActionLevelActorAndDateRange()
    {
        var repo = new FakeSystemLogRepository();
        var day1 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var day5 = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);
        var day10 = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc);

        await repo.AddAsync(MakeLog(SystemLogActions.SyllabusCreated, SystemLogLevels.Information, 1, day1));
        await repo.AddAsync(MakeLog(SystemLogActions.SyllabusArchived, SystemLogLevels.Warning, 1, day5));
        await repo.AddAsync(MakeLog(SystemLogActions.SchoolApproved, SystemLogLevels.Information, 2, day10));

        var handler = new GetSystemLogsListHandler(repo, new FakeUserRepository());

        var byAction = await handler.Handle(new GetSystemLogsRequest { Action = SystemLogActions.SyllabusArchived });
        Assert.Single(byAction.Items);

        var byLevel = await handler.Handle(new GetSystemLogsRequest { Level = SystemLogLevels.Warning });
        Assert.Single(byLevel.Items);

        var byActor = await handler.Handle(new GetSystemLogsRequest { ActorUserId = 2 });
        Assert.Single(byActor.Items);
        Assert.Equal(SystemLogActions.SchoolApproved, byActor.Items[0].Action);

        var byDateRange = await handler.Handle(new GetSystemLogsRequest { From = day1, To = day5 });
        Assert.Equal(2, byDateRange.Items.Count);
    }

    [Fact]
    public async Task GetSystemLogs_SupportsPagination()
    {
        var repo = new FakeSystemLogRepository();
        for (int i = 0; i < 5; i++)
            await repo.AddAsync(MakeLog(SystemLogActions.SyllabusCreated, SystemLogLevels.Information, 1, DateTime.UtcNow.AddMinutes(i)));

        var handler = new GetSystemLogsListHandler(repo, new FakeUserRepository());
        var page1 = await handler.Handle(new GetSystemLogsRequest { PageNumber = 1, PageSize = 2 });
        var page2 = await handler.Handle(new GetSystemLogsRequest { PageNumber = 2, PageSize = 2 });

        Assert.Equal(5, page1.TotalCount);
        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(2, page2.Items.Count);
        Assert.NotEqual(page1.Items[0].Id, page2.Items[0].Id);
    }

    [Fact]
    public async Task GetSystemLogs_FiltersByEntityTypeAndId()
    {
        var repo = new FakeSystemLogRepository();
        await repo.AddAsync(MakeLog(SystemLogActions.SyllabusCreated, SystemLogLevels.Information, 1, DateTime.UtcNow, "Syllabus", "1"));
        await repo.AddAsync(MakeLog(SystemLogActions.SchoolApproved, SystemLogLevels.Information, 1, DateTime.UtcNow, "School", "40"));

        var handler = new GetSystemLogsListHandler(repo, new FakeUserRepository());
        var result = await handler.Handle(new GetSystemLogsRequest { EntityType = "Syllabus", EntityId = "1" });

        Assert.Single(result.Items);
        Assert.Equal(SystemLogActions.SyllabusCreated, result.Items[0].Action);
    }
}
