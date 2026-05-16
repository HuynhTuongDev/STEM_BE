using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STEM.Application.UseCases.Notifications;

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
            await _notificationHandler.MarkAsRead(id, cancellationToken);
            return Ok(new { success = true, message = "Notification marked as read" });
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
            await _notificationHandler.DeleteNotification(id, cancellationToken);
            return Ok(new { success = true, message = "Notification deleted" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
