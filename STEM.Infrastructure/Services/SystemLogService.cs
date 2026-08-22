using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using STEM.Application.Interfaces;
using STEM.Core.Entities.Common;
using STEM.Core.Repository;

namespace STEM.Infrastructure.Services;

public class SystemLogService : ISystemLogService
{
    private readonly ISystemLogRepository _systemLogRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<SystemLogService> _logger;

    public SystemLogService(
        ISystemLogRepository systemLogRepository,
        IHttpContextAccessor httpContextAccessor,
        ILogger<SystemLogService> logger)
    {
        _systemLogRepository = systemLogRepository;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task WriteAsync(
        string level,
        string action,
        int? actorUserId,
        string? actorRole,
        string? entityType,
        string? entityId,
        string description,
        object? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var log = new SystemLog
            {
                Level = SystemLogLevels.All.Contains(level) ? level : SystemLogLevels.Information,
                Action = action,
                ActorUserId = actorUserId,
                ActorRole = actorRole,
                EntityType = entityType,
                EntityId = entityId,
                Description = description,
                MetadataJson = metadata == null ? null : JsonSerializer.Serialize(metadata),
                IpAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
                CreatedAt = DateTime.UtcNow
            };

            await _systemLogRepository.AddAsync(log, cancellationToken);
            await _systemLogRepository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Availability over audit atomicity: a failed audit write must
            // never fail the business operation that triggered it.
            _logger.LogError(ex, "Failed to write SystemLog entry for action {Action}", action);
        }
    }
}
