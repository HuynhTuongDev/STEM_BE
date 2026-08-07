using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STEM.Application.Interfaces;

namespace STEM.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly IFileService _fileService;

    public UploadController(IFileService fileService)
    {
        _fileService = fileService;
    }

    /// <summary>
    /// Upload file lên storage (Supabase hoặc local).
    /// </summary>
    [Authorize(Policy = "MasterOnly")]
    [HttpPost]
    public async Task<IActionResult> UploadFile(
        [FromForm] IFormFile file,
        [FromForm] string type = "general",
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "No file provided." });

            // Validate file size (10MB max)
            if (file.Length > 10 * 1024 * 1024)
                return BadRequest(new { success = false, message = "File size exceeds 10MB limit." });

            // Validate file types
            var allowedTypes = new[] { "application/pdf", "image/jpeg", "image/jpg", "image/png" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
                return BadRequest(new { success = false, message = "Invalid file type. Only PDF, JPEG, JPG, and PNG are allowed." });

            var folderName = $"uploads/{type}";
            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var originalName = file.FileName;

            var url = await _fileService.UploadFileAsync(file, folderName, cancellationToken);

            return Ok(new
            {
                success = true,
                url,
                fileName,
                originalName,
                message = "File uploaded successfully."
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Upload file cho đăng ký trường (không cần auth - public endpoint).
    /// </summary>
    [AllowAnonymous]
    [HttpPost("school-registration")]
    public async Task<IActionResult> UploadSchoolRegistrationFile(
        [FromForm] IFormFile file,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "No file provided." });

            if (file.Length > 10 * 1024 * 1024)
                return BadRequest(new { success = false, message = "File size exceeds 10MB limit." });

            var allowedTypes = new[] { "application/pdf", "image/jpeg", "image/jpg", "image/png" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
                return BadRequest(new { success = false, message = "Invalid file type. Only PDF, JPEG, JPG, and PNG are allowed." });

            var folderName = "uploads/school-registrations";
            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var originalName = file.FileName;

            var url = await _fileService.UploadFileAsync(file, folderName, cancellationToken);

            return Ok(new
            {
                success = true,
                url,
                fileName,
                originalName,
                message = "File uploaded successfully."
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
