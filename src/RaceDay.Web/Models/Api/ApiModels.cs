namespace RaceDay.Web.Models.Api;

// Mirrors of the Part 2 API contracts. The MVC app has no reference to the API project or its
// database: it only knows the JSON shapes it sends and receives over HTTP.

public record LoginRequest(string Email, string Password);

public record LoginResponse(string Token, string Role, int UserId);

public record RegisterRequest(string FullName, string Email, string Password, string Role, string? PhoneNumber);

public record UserDto(int UserId, string FullName, string Email, string Role, string? PhoneNumber, DateTime CreatedAt, string? ProfilePictureUrl);

public record UpdateProfileRequest(string FullName, string? PhoneNumber);

public record VenueDto(int VenueId, string Name, string? AddressLine, string City, string? Province, decimal? Latitude, decimal? Longitude);

public record VenueRequest(string Name, string? AddressLine, string City, string? Province, decimal? Latitude, decimal? Longitude);

public record CategoryDto(int CategoryId, int EventId, string Name, decimal DistanceKm, decimal EntryFee, int MaxParticipants);

public record CategoryRequest(string Name, decimal DistanceKm, decimal EntryFee, int MaxParticipants);

public record EventSummaryDto(
    int EventId, string Title, string? Description, DateOnly EventDate, string EventType,
    int OrganiserId, int VenueId, string VenueName, string City, string? Province, string? BannerImageUrl,
    decimal? MinDistanceKm, decimal? MaxDistanceKm);

public record EventDetailDto(
    int EventId, string Title, string? Description, DateOnly EventDate, string EventType,
    int OrganiserId, VenueDto Venue, List<CategoryDto> Categories, string? BannerImageUrl);

public record EventRequest(string Title, string? Description, DateOnly EventDate, string EventType, int VenueId);

public record OrganiserEventDto(
    int EventId, string Title, DateOnly EventDate, string EventType, string VenueName, string City,
    int CategoryCount, int ConfirmedEnrolments, int TotalCapacity, int ResultsCaptured, string? BannerImageUrl);

public record EnrolResponse(int EnrolmentId, string? BibNumber);

public record EnrolmentDto(
    int EnrolmentId, int ParticipantId, string ParticipantName, int CategoryId, string CategoryName,
    int EventId, string EventTitle, DateOnly EventDate, string? BibNumber, string Status, DateTime EnrolmentDate);

public record ResultRequest(TimeSpan? FinishTime, int? Position, string Status);

public record ResultDto(
    int ResultId, int EnrolmentId, int ParticipantId, string ParticipantName, string? BibNumber,
    int EventId, string EventTitle, string CategoryName, TimeSpan? FinishTime, int? Position, string Status,
    DateOnly EventDate, decimal DistanceKm, int TotalFinishers);

public static class RoleNames
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}

public static class EventTypes
{
    public static readonly string[] All = { "Running", "Walking", "Cycling" };
}
