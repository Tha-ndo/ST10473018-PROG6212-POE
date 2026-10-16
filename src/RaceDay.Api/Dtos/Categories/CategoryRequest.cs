using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public class CategoryRequest
{
    [Required, StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, 999.99)]
    public decimal DistanceKm { get; set; }

    [Range(0, 999999.99)]
    public decimal EntryFee { get; set; }

    [Range(1, 1000000)]
    public int MaxParticipants { get; set; } = 100;
}
