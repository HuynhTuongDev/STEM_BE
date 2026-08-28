using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using STEM.Application.Interfaces;
using STEM.Core.Entities.Common;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Services;

public class ProgressAlertBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProgressAlertBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(6);

    public ProgressAlertBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ProgressAlertBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Progress Alert Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckStudentProgressAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in progress alert background service");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task CheckStudentProgressAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<StemDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var now = DateTime.UtcNow;
        var oneWeekAgo = now.AddDays(-7);

        // Get students with graded submissions in the last week
        var recentSubmissions = await dbContext.Submissions
            .Include(s => s.Assignment)
                .ThenInclude(a => a.Class)
            .Include(s => s.Student)
            .Where(s => s.GradedAt.HasValue && s.GradedAt >= oneWeekAgo && s.FinalScore.HasValue)
            .ToListAsync(stoppingToken);

        // Group by student
        var studentSubmissions = recentSubmissions
            .GroupBy(s => s.StudentId)
            .ToList();

        foreach (var group in studentSubmissions)
        {
            if (!group.Key.HasValue) continue;
            var studentId = group.Key.Value;
            var submissions = group.ToList();
            var totalAssignments = submissions.Count;

            // Get class info for notification
            var classEntity = submissions.FirstOrDefault()?.Assignment?.Class;

            // N-36: Alert teacher when student has new submissions
            if (totalAssignments >= 1 && classEntity != null && classEntity.TeacherId > 0)
            {
                var studentName = submissions.FirstOrDefault()?.Student?.FullName ?? "học sinh";
                var title = "Học sinh đã nộp bài";
                var content = $"{studentName} đã nộp {totalAssignments} bài tập mới. Vui lòng kiểm tra và chấm điểm.";

                await notificationService.SendAsync(classEntity.TeacherId, title, content, NotificationType.StudentProgressAlert, stoppingToken);
            }
        }

        // Check for students with overdue assignments
        var overdueAssignments = await dbContext.Assignments
            .Include(a => a.Class)
                .ThenInclude(c => c.Enrollments)
            .Where(a => a.DueDate.HasValue 
                && a.DueDate.Value < now
                && a.Status == "published")
            .ToListAsync(stoppingToken);

        foreach (var assignment in overdueAssignments)
        {
            if (assignment.Class?.Enrollments == null) continue;

            // Get students who haven't submitted this assignment
            var studentIds = assignment.Class.Enrollments.Select(e => e.StudentId).ToList();
            if (!studentIds.Any()) continue;
            
            var submittedStudentIds = await dbContext.Submissions
                .Where(s => s.AssignmentId == assignment.Id && studentIds.Contains(s.StudentId ?? 0))
                .Select(s => s.StudentId)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToListAsync(stoppingToken);

            var missingStudentIds = studentIds.Except(submittedStudentIds).ToList();

            // N-37: Remind students about overdue assignments
            foreach (var studentId in missingStudentIds)
            {
                var daysOverdue = (int)Math.Ceiling((now - assignment.DueDate!.Value).TotalDays);
                var title = "Bài tập quá hạn";
                var content = $"Bạn chưa nộp bài \"{assignment.Title}\". Đã quá hạn {daysOverdue} ngày!";

                await notificationService.SendAsync(studentId, title, content, NotificationType.ProgressAlert, stoppingToken);
            }
        }

        _logger.LogInformation("Progress alert check completed");
    }
}
