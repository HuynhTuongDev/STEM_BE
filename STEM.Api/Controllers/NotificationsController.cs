using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STEM.Application.UseCases.Notifications;
using System.Security.Claims;
using STEM.Core.Entities.Users;

namespace STEM.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly NotificationHandler _notificationHandler;

    public NotificationsController(NotificationHandler notificationHandler)
    {
        _notificationHandler = notificationHandler;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications(int? userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);
            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserId = int.Parse(currentUserIdStr ?? "0");
            
            Console.WriteLine($"[DEBUG] GetNotifications: token userId={currentUserIdStr}, role={currentUserRole}, param userId={userId}");

            // Use current user's ID if userId not provided, or if not SchoolAdmin
            var targetUserId = (userId ?? currentUserId);
            if (currentUserRole != RoleNames.SchoolAdministrator && targetUserId != currentUserId)
            {
                return Forbid();
            }

            var notifications = await _notificationHandler.GetNotificationsByUserId(targetUserId, page, pageSize, cancellationToken);
            return Ok(new { success = true, data = notifications });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] GetNotifications: {ex}");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPatch("{id}/mark-as-read")]
    public async Task<IActionResult> MarkAsRead(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

            await _notificationHandler.MarkAsRead(id, currentUserId, currentUserRole, cancellationToken);
            return Ok(new { success = true, message = "Notification marked as read" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

            await _notificationHandler.DeleteNotification(id, currentUserId, currentUserRole, cancellationToken);
            return Ok(new { success = true, message = "Notification deleted" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPatch("mark-all-as-read")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

            await _notificationHandler.MarkAllAsRead(currentUserId, cancellationToken);
            return Ok(new { success = true, message = "All notifications marked as read" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
