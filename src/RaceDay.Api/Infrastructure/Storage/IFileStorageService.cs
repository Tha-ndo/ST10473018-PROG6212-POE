namespace RaceDay.Api.Storage;

/// <summary>
/// Abstraction over cloud file storage. The API is the only component that talks to Azure;
/// the MVC front end uploads files to the API, which stores them here.
/// </summary>
public interface IFileStorageService
{
    /// <summary>Uploads an image and returns its public URL.</summary>
    /// <param name="content">The file stream.</param>
    /// <param name="contentType">MIME type, e.g. image/jpeg.</param>
    /// <param name="folder">Virtual folder inside the container, e.g. "event-banners".</param>
    /// <param name="extension">File extension including the dot, e.g. ".jpg".</param>
    Task<string> UploadAsync(Stream content, string contentType, string folder, string extension, CancellationToken ct = default);

    /// <summary>Deletes a previously uploaded file. Does nothing if the URL is empty or not one of ours.</summary>
    Task DeleteAsync(string? url, CancellationToken ct = default);
}
