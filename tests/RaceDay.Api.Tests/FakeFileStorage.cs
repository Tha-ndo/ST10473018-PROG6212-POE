using RaceDay.Api.Storage;

namespace RaceDay.Api.Tests;

/// <summary>In-memory stand-in for Azure Blob Storage so tests never need a real storage account.</summary>
public class FakeFileStorage : IFileStorageService
{
    public const string BaseUrl = "https://fake.blob.core.windows.net/raceday-images/";

    public List<string> Uploaded { get; } = new();
    public List<string> Deleted { get; } = new();

    public Task<string> UploadAsync(Stream content, string contentType, string folder, string extension, CancellationToken ct = default)
    {
        var url = $"{BaseUrl}{folder}/{Guid.NewGuid():N}{extension}";
        lock (Uploaded) Uploaded.Add(url);
        return Task.FromResult(url);
    }

    public Task DeleteAsync(string? url, CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(url)) lock (Deleted) Deleted.Add(url);
        return Task.CompletedTask;
    }
}
