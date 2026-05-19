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
}
