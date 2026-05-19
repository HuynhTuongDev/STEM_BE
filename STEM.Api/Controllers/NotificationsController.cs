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
    public async Task<IActionResult> GetNotifications(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Access Control: 
            // 1. School Admin can access any user's notifications
            // 2. Other users (e.g. Students) can only access their own notifications
            if (currentUserRole != RoleNames.SchoolAdministrator && currentUserId != userId.ToString())
            {
                return Forbid();
            }

            var notifications = await _notificationHandler.GetNotificationsByUserId(userId, cancellationToken);
            return Ok(new { success = true, data = notifications });
        }
        catch (Exception ex)
        {
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
        catch (UnauthorizedAccessException ex)
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
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
