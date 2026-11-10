using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public record LoginResponse(string Token, string Role, int UserId);
