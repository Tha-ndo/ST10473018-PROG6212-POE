namespace RaceDay.Api.Dtos;

/// <summary>Row on the Organiser dashboard: one of the caller's own events with enrolment totals.</summary>
public record OrganiserEventDto(
    int EventId, string Title, DateOnly EventDate, string EventType, string VenueName, string City,
    int CategoryCount, int ConfirmedEnrolments, int TotalCapacity, int ResultsCaptured, string? BannerImageUrl);
