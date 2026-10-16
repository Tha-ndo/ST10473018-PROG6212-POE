using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public record UserDto(int UserId, string FullName, string Email, string Role, string? PhoneNumber, DateTime CreatedAt);
