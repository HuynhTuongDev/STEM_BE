using Microsoft.AspNetCore.Http;

namespace STEM.Application.Interfaces;

public interface IFileService
{
    /// <summary>
    /// Uploads a file to the storage service and returns its public URL.
    /// </summary>
    /// <param name="file">The file to upload</param>
    /// <param name="folderName">The target folder/bucket name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The public URL of the uploaded file</returns>
    Task<string> UploadFileAsync(IFormFile file, string folderName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from the storage service by its URL.
    /// </summary>
    /// <param name="fileUrl">The public URL of the file to delete</param>
    /// <param name="bucketName">The bucket name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteFileAsync(string fileUrl, string bucketName, CancellationToken cancellationToken = default);
}
