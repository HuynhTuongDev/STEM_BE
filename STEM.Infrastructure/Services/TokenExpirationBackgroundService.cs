using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using STEM.Application.UseCases.Payments;

namespace STEM.Infrastructure.Services;

public class TokenExpirationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TokenExpirationBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(30);

    public TokenExpirationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<TokenExpirationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Token Expiration Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndRevokeExpiredAllocations(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in token expiration background service");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task CheckAndRevokeExpiredAllocations(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var revokeHandler = scope.ServiceProvider.GetRequiredService<RevokeExpiredAllocationsHandler>();

        _logger.LogDebug("Checking for expired token allocations...");
        var result = await revokeHandler.Handle(stoppingToken);

        if (result.SuccessCount > 0)
        {
            _logger.LogInformation("Revoked {Count} expired allocations, returned {Tokens} tokens",
                result.SuccessCount, result.TotalTokensReturned);
        }
    }
}
