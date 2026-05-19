using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using STEM.Application.Dtos.Users;
using STEM.Application.UseCases.Users;
using System.Security.Claims;

namespace STEM.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly GetUserProfileHandler _getUserProfileHandler;
    private readonly UpdateUserProfileHandler _updateUserProfileHandler;
    private readonly UploadAvatarHandler _uploadAvatarHandler;

    public UsersController(
        GetUserProfileHandler getUserProfileHandler,
        UpdateUserProfileHandler updateUserProfileHandler,
        UploadAvatarHandler uploadAvatarHandler)
    {
        _getUserProfileHandler = getUserProfileHandler;
        _updateUserProfileHandler = updateUserProfileHandler;
        _uploadAvatarHandler = uploadAvatarHandler;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            var profile = await _getUserProfileHandler.Handle(userId, cancellationToken);
            return Ok(new { success = true, data = profile });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while fetching the profile.", error = ex.Message });
        }
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            var updatedProfile = await _updateUserProfileHandler.Handle(userId, request, cancellationToken);
            return Ok(new { success = true, data = updatedProfile, message = "Profile updated successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while updating the profile.", error = ex.Message });
        }
    }

    [HttpPost("avatar")]
    public async Task<IActionResult> UploadAvatar(IFormFile file, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            var avatarUrl = await _uploadAvatarHandler.Handle(userId, file, cancellationToken);
            return Ok(new { success = true, data = new { avatarUrl }, message = "Avatar uploaded successfully." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while uploading the avatar.", error = ex.Message });
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out int userId))
        {
            return userId;
        }
        throw new UnauthorizedAccessException("User is not authenticated properly.");
    }
}
