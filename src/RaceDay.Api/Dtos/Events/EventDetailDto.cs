using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public record EventDetailDto(
    int EventId, string Title, string? Description, DateOnly EventDate, string EventType,
    int OrganiserId, VenueDto Venue, IReadOnlyList<CategoryDto> Categories);
