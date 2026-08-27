using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using STEM.Application.Interfaces;
using STEM.Core.Entities.Common;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Services;

public class NotificationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(15);

    public NotificationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<NotificationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendDueDateRemindersAsync(stoppingToken);
                await SendScheduleRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in notification background service");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task SendDueDateRemindersAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<StemDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var now = DateTime.UtcNow;

        // N-21, N-22: Assignment/VirtualLab Due Soon (24 hours before)
        var tomorrow = now.AddHours(24);
        var dueSoonAssignments = await dbContext.Assignments
            .Include(a => a.Class)
                .ThenInclude(c => c.Enrollments)
            .Where(a => a.DueDate.HasValue 
                && a.DueDate.Value > now 
                && a.DueDate.Value <= tomorrow
                && a.Status == "published")
            .ToListAsync(stoppingToken);

        foreach (var assignment in dueSoonAssignments)
        {
            if (assignment.Class?.Enrollments == null) continue;

            var notificationType = assignment.AssignmentType?.ToLower() switch
            {
                "practicalsimulation" => NotificationType.VirtualLabDueSoon,
                _ => NotificationType.AssignmentDueSoon
            };

            var assignmentTypeText = assignment.AssignmentType?.ToLower() switch
            {
                "practicalsimulation" => "Virtual Lab",
                "quiz" => "bài Quiz",
                "report" => "bài báo cáo",
                _ => "bài tập"
            };

            var studentIds = assignment.Class.Enrollments.Select(e => e.StudentId).ToList();
            var hoursRemaining = (int)Math.Ceiling((assignment.DueDate!.Value - now).TotalHours);

            var title = $"{assignmentTypeText} sắp đến hạn";
            var content = $"\"{assignment.Title}\" sắp hết hạn trong {hoursRemaining} giờ!";

            await notificationService.SendToManyAsync(studentIds, title, content, notificationType, stoppingToken);
        }

        // N-32, N-33: Assignment/VirtualLab Due Today
        var endOfToday = now.Date.AddDays(1);
        var dueTodayAssignments = await dbContext.Assignments
            .Include(a => a.Class)
                .ThenInclude(c => c.Enrollments)
            .Where(a => a.DueDate.HasValue
                && a.DueDate.Value > now
                && a.DueDate.Value <= endOfToday
                && a.Status == "published")
            .ToListAsync(stoppingToken);

        foreach (var assignment in dueTodayAssignments)
        {
            if (assignment.Class?.Enrollments == null) continue;

            var notificationType = assignment.AssignmentType?.ToLower() switch
            {
                "practicalsimulation" => NotificationType.VirtualLabDueToday,
                _ => NotificationType.AssignmentDueToday
            };

            var assignmentTypeText = assignment.AssignmentType?.ToLower() switch
            {
                "practicalsimulation" => "Virtual Lab",
                "quiz" => "bài Quiz",
                "report" => "bài báo cáo",
                _ => "bài tập"
            };

            var studentIds = assignment.Class.Enrollments.Select(e => e.StudentId).ToList();
            var timeRemaining = assignment.DueDate!.Value - now;
            var timeText = timeRemaining.TotalHours >= 1
                ? $"{(int)timeRemaining.TotalHours} giờ"
                : $"{(int)timeRemaining.TotalMinutes} phút";

            var title = $"{assignmentTypeText} đến hạn hôm nay";
            var content = $"\"{assignment.Title}\" đến hạn trong {timeText}!";

            await notificationService.SendToManyAsync(studentIds, title, content, notificationType, stoppingToken);
        }

        if (dueSoonAssignments.Count > 0 || dueTodayAssignments.Count > 0)
        {
            _logger.LogInformation("Sent {DueSoon} due-soon and {DueToday} due-today notifications",
                dueSoonAssignments.Count, dueTodayAssignments.Count);
        }
    }

    private async Task SendScheduleRemindersAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<StemDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var now = DateTime.UtcNow;

        // N-31: Schedule Reminder (30 minutes before class)
        var in30Minutes = now.AddMinutes(30);
        var in31Minutes = now.AddMinutes(31);

        var upcomingSchedules = await dbContext.Schedules
            .Include(s => s.Class)
                .ThenInclude(c => c.Enrollments)
            .Include(s => s.Lesson)
            .Where(s => s.StartTime >= in30Minutes && s.StartTime < in31Minutes)
            .ToListAsync(stoppingToken);

        foreach (var schedule in upcomingSchedules)
        {
            if (schedule.Class?.Enrollments == null) continue;

            var lessonName = schedule.Lesson?.Title ?? "buổi học";
            var classCode = schedule.Class.ClassCode;
            var startTime = schedule.StartTime.ToLocalTime().ToString("HH:mm");

            var studentIds = schedule.Class.Enrollments.Select(e => e.StudentId).ToList();

            var title = "Nhắc lịch học";
            var content = $"Lớp {classCode} - {lessonName} bắt đầu lúc {startTime}!";

            await notificationService.SendToManyAsync(studentIds, title, content, NotificationType.ScheduleReminder, stoppingToken);
        }

        if (upcomingSchedules.Count > 0)
        {
            _logger.LogInformation("Sent schedule reminders to {Count} classes", upcomingSchedules.Count);
        }
    }
}
