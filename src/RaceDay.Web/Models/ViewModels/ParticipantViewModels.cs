using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Models.ViewModels;

public class EventBrowseViewModel
{
    public string? Type { get; set; }
    public string? Province { get; set; }
    public string? Search { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }

    public List<EventSummaryDto> Events { get; set; } = new();
    public List<string> Provinces { get; set; } = new();
    public HashSet<int> EnrolledEventIds { get; set; } = new();

    public bool HasFilters => !string.IsNullOrWhiteSpace(Type) || !string.IsNullOrWhiteSpace(Province)
                              || !string.IsNullOrWhiteSpace(Search) || From is not null || To is not null;
}

public class EventDetailsViewModel
{
    public EventDetailDto Event { get; set; } = null!;
    public List<ResultDto> Results { get; set; } = new();

    /// <summary>The logged-in Participant's enrolments for this event (if any).</summary>
    public List<EnrolmentDto> MyEnrolments { get; set; } = new();

    public bool IsPast => Event.EventDate < DateOnly.FromDateTime(DateTime.Today);

    public EnrolmentDto? ActiveEnrolment => MyEnrolments.FirstOrDefault(e => e.Status == "Confirmed");
}

public class MyEnrolmentsViewModel
{
    public List<EnrolmentDto> Upcoming { get; set; } = new();
    public List<EnrolmentDto> Past { get; set; } = new();
    public List<EnrolmentDto> Cancelled { get; set; } = new();
}

public class MyResultsViewModel
{
    public List<ResultDto> Results { get; set; } = new();

    public int RacesFinished => Results.Count(r => r.Status == "Finished");
    public decimal DistanceCovered => Results.Where(r => r.Status == "Finished").Sum(r => r.DistanceKm);
    public int? BestPosition => Results.Where(r => r.Position is not null).Min(r => r.Position);
}
