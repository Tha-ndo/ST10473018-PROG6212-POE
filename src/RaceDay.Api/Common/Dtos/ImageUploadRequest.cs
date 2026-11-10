using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

/// <summary>multipart/form-data body with a single image file named "file".</summary>
public class ImageUploadRequest
{
    [Required]
    public IFormFile? File { get; set; }
}
