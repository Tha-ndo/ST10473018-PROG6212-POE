namespace RaceDay.Api.Models;

public class AppUser
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = Roles.Participant;
    public string? PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<RaceEvent> OrganisedEvents { get; set; } = new List<RaceEvent>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
