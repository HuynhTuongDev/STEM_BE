using Microsoft.AspNetCore.Http;
using STEM.Application.Interfaces;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Users;

public class UploadAvatarHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IFileService _fileService;

    public UploadAvatarHandler(IUserRepository userRepository, IFileService fileService)
    {
        _userRepository = userRepository;
        _fileService = fileService;
    }

    public async Task<string> Handle(int userId, IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is empty or not provided.");
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            throw new KeyNotFoundException("User not found.");

        // Check if file is an image (basic check)
        if (!file.ContentType.StartsWith("image/"))
        {
            throw new ArgumentException("Only image files are allowed.");
        }

        // Check file size (e.g., max 5MB)
        if (file.Length > 5 * 1024 * 1024)
        {
            throw new ArgumentException("File size must not exceed 5MB.");
        }

        // Upload to Supabase Storage
        var publicUrl = await _fileService.UploadFileAsync(file, "avatars", cancellationToken);

        // Update user
        user.Avatar = publicUrl;
        user.UpdatedAt = DateTime.UtcNow;
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return publicUrl;
    }
}
