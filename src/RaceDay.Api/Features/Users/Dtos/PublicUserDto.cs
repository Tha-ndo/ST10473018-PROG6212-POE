using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public record PublicUserDto(int UserId, string FullName, string Role);
