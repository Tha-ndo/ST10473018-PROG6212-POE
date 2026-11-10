namespace RaceDay.Api.Models;

public class RaceEvent
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public int VenueId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly EventDate { get; set; }
    public string EventType { get; set; } = "Running";
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>Public URL of the event banner stored in Azure Blob Storage (Part 3).</summary>
    public string? BannerImageUrl { get; set; }

    public AppUser Organiser { get; set; } = null!;
    public Venue Venue { get; set; } = null!;
    public ICollection<Category> Categories { get; set; } = new List<Category>();
}
