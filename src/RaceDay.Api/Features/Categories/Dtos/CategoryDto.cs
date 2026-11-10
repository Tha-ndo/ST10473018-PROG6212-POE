using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public record CategoryDto(int CategoryId, int EventId, string Name, decimal DistanceKm, decimal EntryFee, int MaxParticipants);
