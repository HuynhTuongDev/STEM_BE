using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STEM.Application.Interfaces;
using STEM.Core.Entities.Projects;
using STEM.Core.Repository;

namespace STEM.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly IFileService _fileService;
    private readonly IFileRepository _fileRepository;

    public UploadController(IFileService fileService, IFileRepository fileRepository)
    {
        _fileService = fileService;
        _fileRepository = fileRepository;
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

            if (file.Length > 10 * 1024 * 1024)
                return BadRequest(new { success = false, message = "File size exceeds 10MB limit." });

            var allowedTypes = new[] { 
                "application/pdf", 
                "image/jpeg", 
                "image/jpg", 
                "image/png",
                "application/msword",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
            };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
                return BadRequest(new { success = false, message = "Invalid file type. Only PDF, JPEG, JPG, PNG, DOC, and DOCX are allowed." });

            var folderName = $"uploads/{type}";
            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var originalName = file.FileName;

            var url = await _fileService.UploadFileAsync(file, folderName, cancellationToken);

            var submissionFile = new SubmissionFile
            {
                Url = url,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _fileRepository.AddAsync(submissionFile, cancellationToken);
            await _fileRepository.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                success = true,
                url,
                fileName,
                originalName,
                fileId = submissionFile.Id,
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

            var allowedTypes = new[] { 
                "application/pdf", 
                "image/jpeg", 
                "image/jpg", 
                "image/png",
                "application/msword",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
            };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
                return BadRequest(new { success = false, message = "Invalid file type. Only PDF, JPEG, JPG, PNG, DOC, and DOCX are allowed." });

            var folderName = "uploads/school-registrations";
            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var originalName = file.FileName;

            var url = await _fileService.UploadFileAsync(file, folderName, cancellationToken);

            var submissionFile = new SubmissionFile
            {
                Url = url,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _fileRepository.AddAsync(submissionFile, cancellationToken);
            await _fileRepository.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                success = true,
                url,
                fileName,
                originalName,
                fileId = submissionFile.Id,
                message = "File uploaded successfully."
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Upload file cho bài tập (học sinh có thể upload).
    /// </summary>
    [Authorize]
    [HttpPost("assignment")]
    public async Task<IActionResult> UploadAssignmentFile(
        [FromForm] IFormFile file,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "No file provided." });

            if (file.Length > 10 * 1024 * 1024)
                return BadRequest(new { success = false, message = "File size exceeds 10MB limit." });

            var allowedTypes = new[] { 
                "application/pdf", 
                "image/jpeg", 
                "image/jpg", 
                "image/png",
                "application/msword",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
            };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
                return BadRequest(new { success = false, message = "Invalid file type. Only PDF, JPEG, JPG, PNG, DOC, and DOCX are allowed." });

            var folderName = "uploads/assignments";
            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var originalName = file.FileName;

            var url = await _fileService.UploadFileAsync(file, folderName, cancellationToken);

            var submissionFile = new SubmissionFile
            {
                Url = url,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _fileRepository.AddAsync(submissionFile, cancellationToken);
            await _fileRepository.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                success = true,
                url,
                fileName,
                originalName,
                fileId = submissionFile.Id,
                message = "File uploaded successfully."
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Download file bằng fileId (dành cho giáo viên).
    /// </summary>
    [Authorize]
    [HttpGet("download/{fileId}")]
    public async Task<IActionResult> DownloadFile(int fileId, CancellationToken cancellationToken = default)
    {
        var submissionFile = await _fileRepository.GetByIdAsync(fileId, cancellationToken);
        if (submissionFile == null)
            return NotFound(new { success = false, message = "File not found." });

        if (string.IsNullOrEmpty(submissionFile.Url))
            return NotFound(new { success = false, message = "File URL not found." });

        return Redirect(submissionFile.Url);
    }
}
