using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using STEM.Application.Interfaces;
using Supabase.Storage;

namespace STEM.Infrastructure.Services;

public class SupabaseStorageService : IFileService
{
    private readonly Supabase.Client _supabaseClient;
    private readonly IConfiguration _configuration;

    public SupabaseStorageService(Supabase.Client supabaseClient, IConfiguration configuration)
    {
        _supabaseClient = supabaseClient;
        _configuration = configuration;
    }

    public async Task<string> UploadFileAsync(IFormFile file, string folderName, CancellationToken cancellationToken = default)
    {
        var bucketName = _configuration["Supabase:BucketName"] ?? "avatars";

        // Generate a unique file name
        var fileExtension = Path.GetExtension(file.FileName);
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
        var supabasePath = $"{folderName}/{uniqueFileName}";

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        var fileBytes = memoryStream.ToArray();

        // Upload to Supabase
        var storage = _supabaseClient.Storage;
        var bucket = storage.From(bucketName);

        await bucket.Upload(fileBytes, supabasePath, new Supabase.Storage.FileOptions
        {
            ContentType = file.ContentType,
            Upsert = true
        });

        // Get public URL
        var publicUrl = bucket.GetPublicUrl(supabasePath);

        return publicUrl;
    }

    public async Task DeleteFileAsync(string fileUrl, string bucketName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            return;

        try
        {
            // Extract file path from URL
            // URL format: https://xvookhjvebxszqfdfuen.supabase.co/storage/v1/object/public/avatars/folder/uuid.jpg
            var uri = new Uri(fileUrl);
            var pathParts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            // Find "public" index and get everything after it
            var publicIndex = Array.IndexOf(pathParts, "public");
            if (publicIndex == -1 || publicIndex + 2 >= pathParts.Length)
                return;

            // Reconstruct path: folder/uuid.jpg
            var filePath = string.Join("/", pathParts.Skip(publicIndex + 2));

            var storage = _supabaseClient.Storage;
            var bucket = storage.From(bucketName);

            await bucket.Remove(new List<string> { filePath });
        }
        catch (Exception ex)
        {
            // Log error but don't throw - file deletion failure shouldn't crash the app
            // Could add ILogger here if needed
            System.Diagnostics.Debug.WriteLine($"Error deleting file from storage: {ex.Message}");
        }
    }
}