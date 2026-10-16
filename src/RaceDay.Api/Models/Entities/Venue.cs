namespace RaceDay.Api.Models;

public class Venue
{
    public int VenueId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AddressLine { get; set; }
    public string City { get; set; } = string.Empty;
    public string? Province { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public ICollection<RaceEvent> Events { get; set; } = new List<RaceEvent>();
}
