using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public class RegisterRequest
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Required, RegularExpression("^(Organiser|Participant)$", ErrorMessage = "Role must be either 'Organiser' or 'Participant'.")]
    public string Role { get; set; } = string.Empty;

    [StringLength(20)]
    public string? PhoneNumber { get; set; }
}
