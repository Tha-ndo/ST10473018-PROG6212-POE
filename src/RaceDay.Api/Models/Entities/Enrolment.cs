namespace RaceDay.Api.Models;

public class Enrolment
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public int CategoryId { get; set; }
    public DateTime EnrolmentDate { get; set; } = DateTime.Now;
    public string? BibNumber { get; set; }
    public string Status { get; set; } = EnrolmentStatus.Confirmed;

    public AppUser Participant { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public RaceResult? Result { get; set; }
}
