using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public record VenueDto(int VenueId, string Name, string? AddressLine, string City, string? Province, decimal? Latitude, decimal? Longitude);
