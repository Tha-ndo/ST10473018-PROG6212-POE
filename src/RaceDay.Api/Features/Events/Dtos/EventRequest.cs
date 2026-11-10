using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public class EventRequest
{
    [Required, StringLength(150)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public DateOnly? EventDate { get; set; }

    [Required, RegularExpression("^(Running|Walking|Cycling)$", ErrorMessage = "EventType must be Running, Walking or Cycling.")]
    public string EventType { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "A valid VenueId is required.")]
    public int VenueId { get; set; }
}
