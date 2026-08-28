using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using STEM.Application.Interfaces;
using STEM.Core.Entities.Common;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Services;

public class SchoolAdminNotificationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SchoolAdminNotificationBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

    public SchoolAdminNotificationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<SchoolAdminNotificationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("School Admin Notification Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckTokenBalanceAsync(stoppingToken);
                await CheckSubscriptionExpirationAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in school admin notification background service");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task CheckTokenBalanceAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<StemDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        // N-34: Token Balance Low (when balance < 20% of allocated)
        var schools = await dbContext.Schools
            .Include(s => s.TokenAccounts)
            .ToListAsync(stoppingToken);

        foreach (var school in schools)
        {
            var tokenAccount = school.TokenAccounts.FirstOrDefault();
            if (tokenAccount == null || !school.AdminId.HasValue) continue;

            var adminId = school.AdminId.Value;

            // Check if we've already notified in the last 24 hours (to avoid spam)
            var recentNotification = await dbContext.Notifications
                .Where(n => n.UserId == adminId
                    && n.Type == NotificationType.TokenBalanceLow
                    && n.CreatedAt >= DateTime.UtcNow.AddHours(-24))
                .AnyAsync(stoppingToken);

            if (recentNotification) continue;

            // Check if balance is low (less than 20% remaining)
            if (tokenAccount.TotalAllocated > 0)
            {
                var usedPercentage = (tokenAccount.TotalAllocated - tokenAccount.Balance) / tokenAccount.TotalAllocated * 100;
                
                if (usedPercentage >= 80)
                {
                    var remainingTokens = tokenAccount.Balance;
                    var title = "Cảnh báo số dư Token";
                    var content = $"Số dư Token của trường chỉ còn {remainingTokens:N0}. Vui lòng nạp thêm!";

                    await notificationService.SendAsync(adminId, title, content, NotificationType.TokenBalanceLow, stoppingToken);
                    
                    _logger.LogInformation("Sent token balance warning to school {SchoolId}", school.Id);
                }
            }
        }
    }

    private async Task CheckSubscriptionExpirationAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<StemDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var now = DateTime.UtcNow;

        // N-35: Subscription Expiring Soon (7 days before)
        var in7Days = now.AddDays(7);
        var schoolsExpiringSoon = await dbContext.Schools
            .Where(s => s.SubscriptionEndDate.HasValue
                && s.SubscriptionEndDate.Value > now
                && s.SubscriptionEndDate.Value <= in7Days)
            .ToListAsync(stoppingToken);

        foreach (var school in schoolsExpiringSoon)
        {
            if (!school.AdminId.HasValue) continue;
            var adminId = school.AdminId.Value;

            // Check if we've already notified
            var recentNotification = await dbContext.Notifications
                .Where(n => n.UserId == adminId
                    && n.Type == NotificationType.SubscriptionExpiring
                    && n.CreatedAt >= DateTime.UtcNow.AddDays(-1))
                .AnyAsync(stoppingToken);

            if (recentNotification) continue;

            var daysRemaining = (int)Math.Ceiling((school.SubscriptionEndDate!.Value - now).TotalDays);
            var title = "Subscription sắp hết hạn";
            var content = $"Subscription của trường sẽ hết hạn sau {daysRemaining} ngày. Vui lòng gia hạn!";

            await notificationService.SendAsync(adminId, title, content, NotificationType.SubscriptionExpiring, stoppingToken);
            
            _logger.LogInformation("Sent subscription expiring warning to school {SchoolId}", school.Id);
        }

        // N-35: Subscription Expired Today
        var startOfToday = now.Date;
        var endOfToday = startOfToday.AddDays(1);
        var schoolsExpiredToday = await dbContext.Schools
            .Where(s => s.SubscriptionEndDate.HasValue
                && s.SubscriptionEndDate.Value >= startOfToday
                && s.SubscriptionEndDate.Value < endOfToday)
            .ToListAsync(stoppingToken);

        foreach (var school in schoolsExpiredToday)
        {
            if (!school.AdminId.HasValue) continue;
            var adminId = school.AdminId.Value;

            // Check if we've already notified
            var recentNotification = await dbContext.Notifications
                .Where(n => n.UserId == adminId
                    && n.Type == NotificationType.SubscriptionExpiring
                    && n.CreatedAt >= startOfToday)
                .AnyAsync(stoppingToken);

            if (recentNotification) continue;

            var title = "Subscription đã hết hạn";
            var content = $"Subscription của trường đã hết hạn. Vui lòng gia hạn để tiếp tục sử dụng dịch vụ.";

            await notificationService.SendAsync(adminId, title, content, NotificationType.SubscriptionExpiring, stoppingToken);
            
            _logger.LogInformation("Sent subscription expired notification to school {SchoolId}", school.Id);
        }
    }
}
