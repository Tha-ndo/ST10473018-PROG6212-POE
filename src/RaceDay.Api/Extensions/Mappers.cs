using System.Linq.Expressions;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Extensions;

public static class Mappers
{
    public static UserDto ToDto(this AppUser u) =>
        new(u.UserId, u.FullName, u.Email, u.Role, u.PhoneNumber, u.CreatedAt);

    public static VenueDto ToDto(this Venue v) =>
        new(v.VenueId, v.Name, v.AddressLine, v.City, v.Province, v.Latitude, v.Longitude);

    public static CategoryDto ToDto(this Category c) =>
        new(c.CategoryId, c.EventId, c.Name, c.DistanceKm, c.EntryFee, c.MaxParticipants);

    public static EventDetailDto ToDetailDto(this RaceEvent e) =>
        new(e.EventId, e.Title, e.Description, e.EventDate, e.EventType, e.OrganiserId,
            e.Venue.ToDto(),
            e.Categories.OrderBy(c => c.DistanceKm).Select(c => c.ToDto()).ToList());

    public static readonly Expression<Func<Enrolment, EnrolmentDto>> EnrolmentProjection = e =>
        new EnrolmentDto(
            e.EnrolmentId, e.ParticipantId, e.Participant.FullName, e.CategoryId, e.Category.Name,
            e.Category.EventId, e.Category.Event.Title, e.Category.Event.EventDate,
            e.BibNumber, e.Status, e.EnrolmentDate);

    public static readonly Expression<Func<RaceResult, ResultDto>> ResultProjection = r =>
        new ResultDto(
            r.ResultId, r.EnrolmentId, r.Enrolment.ParticipantId, r.Enrolment.Participant.FullName,
            r.Enrolment.BibNumber, r.Enrolment.Category.EventId, r.Enrolment.Category.Event.Title,
            r.Enrolment.Category.Name, r.FinishTime, r.Position, r.Status);
}
