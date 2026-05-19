using Microsoft.AspNetCore.Http;
using STEM.Application.Interfaces;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Users;

public class UploadAvatarHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IRepository<UserProfile> _userProfileRepository;
    private readonly IFileService _fileService;

    public UploadAvatarHandler(
        IUserRepository userRepository,
        IRepository<UserProfile> userProfileRepository,
        IFileService fileService)
    {
        _userRepository = userRepository;
        _userProfileRepository = userProfileRepository;
        _fileService = fileService;
    }

    public async Task<string> Handle(int userId, IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty or not provided.");

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            throw new KeyNotFoundException("User not found.");

        if (!file.ContentType.StartsWith("image/"))
            throw new ArgumentException("Only image files are allowed.");

        if (file.Length > 5 * 1024 * 1024)
            throw new ArgumentException("File size must not exceed 5MB.");

        var profile = user.Profile
            ?? (await _userProfileRepository.FindAsync(p => p.UserId == userId, cancellationToken)).FirstOrDefault();

        if (profile == null)
        {
            profile = new UserProfile
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _userProfileRepository.AddAsync(profile, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(profile.Avatar))
            await _fileService.DeleteFileAsync(profile.Avatar, "avatars", cancellationToken);

        var publicUrl = await _fileService.UploadFileAsync(file, "avatars", cancellationToken);

        profile.Avatar = publicUrl;
        profile.UpdatedAt = DateTime.UtcNow;
        _userProfileRepository.Update(profile);
        await _userProfileRepository.SaveChangesAsync(cancellationToken);

        return publicUrl;
    }
}
