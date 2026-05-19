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
}
