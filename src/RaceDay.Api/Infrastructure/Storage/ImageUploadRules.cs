namespace RaceDay.Api.Storage;

/// <summary>Server-side rules for uploaded images (the single source of truth; the MVC app does not duplicate them).</summary>
public static class ImageUploadRules
{
    public const long MaxBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly Dictionary<string, string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp",
        [".gif"] = "image/gif",
    };

    /// <summary>Returns an error message, or null if the file is acceptable.</summary>
    public static string? Validate(IFormFile? file, out string extension, out string contentType)
    {
        extension = string.Empty;
        contentType = string.Empty;

        if (file is null || file.Length == 0) return "Please choose an image to upload.";
        if (file.Length > MaxBytes) return "Images must be 5 MB or smaller.";

        extension = Path.GetExtension(file.FileName);
        if (!AllowedTypes.TryGetValue(extension, out var expected))
            return "Only JPG, PNG, WEBP or GIF images are allowed.";

        contentType = expected;
        return null;
    }
}
