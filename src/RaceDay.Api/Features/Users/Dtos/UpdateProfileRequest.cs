using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public class UpdateProfileRequest
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(20)]
    public string? PhoneNumber { get; set; }
}
