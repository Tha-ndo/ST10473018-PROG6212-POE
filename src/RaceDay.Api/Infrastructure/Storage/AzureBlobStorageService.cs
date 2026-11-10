using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace RaceDay.Api.Storage;

public class AzureStorageOptions
{
    public const string SectionName = "AzureStorage";

    /// <summary>Storage account connection string. "UseDevelopmentStorage=true" targets the Azurite emulator.</summary>
    public string ConnectionString { get; set; } = "UseDevelopmentStorage=true";

    /// <summary>Blob container that holds all RaceDay images.</summary>
    public string ContainerName { get; set; } = "raceday-images";
}

/// <summary>Stores images in Azure Blob Storage using the official Azure.Storage.Blobs SDK.</summary>
public class AzureBlobStorageService : IFileStorageService
{
    private readonly Lazy<BlobContainerClient> _container;
    private readonly ILogger<AzureBlobStorageService> _logger;
    private bool _containerReady;

    public AzureBlobStorageService(IOptions<AzureStorageOptions> options, ILogger<AzureBlobStorageService> logger)
    {
        _logger = logger;
        // Created on first use, so a mistyped connection string only affects uploads, not the rest of the API.
        _container = new Lazy<BlobContainerClient>(() =>
            new BlobServiceClient(options.Value.ConnectionString).GetBlobContainerClient(options.Value.ContainerName));
    }

    private BlobContainerClient Container => _container.Value;

    public async Task<string> UploadAsync(Stream content, string contentType, string folder, string extension, CancellationToken ct = default)
    {
        await EnsureContainerAsync(ct);

        // A GUID name avoids collisions and stops users guessing other people's file names.
        var blobName = $"{folder}/{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var blob = Container.GetBlobClient(blobName);

        await blob.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType, CacheControl = "public, max-age=86400" }
        }, ct);

        _logger.LogInformation("Uploaded {BlobName} to container {Container}", blobName, Container.Name);
        return blob.Uri.ToString();
    }

    public async Task DeleteAsync(string? url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        try
        {
            var containerUri = Container.Uri.ToString().TrimEnd('/') + "/";
            if (!url.StartsWith(containerUri, StringComparison.OrdinalIgnoreCase)) return;

            var blobName = Uri.UnescapeDataString(url[containerUri.Length..]);
            await Container.DeleteBlobIfExistsAsync(blobName, cancellationToken: ct);
        }
        catch (Exception ex) when (ex is RequestFailedException or FormatException or ArgumentException)
        {
            // Failing to clean up an old image must never break the user's request.
            _logger.LogWarning(ex, "Could not delete old blob {Url}", url);
        }
    }

    private async Task EnsureContainerAsync(CancellationToken ct)
    {
        if (_containerReady) return;
        try
        {
            // Blob-level public read so browsers can display banners and profile pictures directly.
            await Container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "PublicAccessNotPermitted")
        {
            _logger.LogError("The storage account does not allow anonymous blob access. " +
                             "Enable 'Allow Blob anonymous access' on the storage account so images can be displayed.");
            await Container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
        }
        _containerReady = true;
    }
}
