using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Extensions;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Browse events (public) and manage them (Organisers, owner only).</summary>
[ApiController]
[Route("api/events")]
[Produces("application/json")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EventsController(RaceDayDbContext db) => _db = db;

    /// <summary>Lists all upcoming events so anyone can browse without logging in.</summary>
    /// <response code="200">Array of upcoming events (today onwards), soonest first.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<EventSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EventSummaryDto>>> GetUpcomingEvents()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var events = await _db.Events.AsNoTracking()
            .Where(e => e.EventDate >= today)
            .OrderBy(e => e.EventDate)
            .Select(e => new EventSummaryDto(
                e.EventId, e.Title, e.Description, e.EventDate, e.EventType,
                e.OrganiserId, e.VenueId, e.Venue.Name, e.Venue.City, e.Venue.Province))
            .ToListAsync();
        return Ok(events);
    }

    /// <summary>Returns full detail for one event, including venue/route information and its categories.</summary>
    /// <param name="id">The event id.</param>
    /// <response code="200">Event detail.</response>
    /// <response code="404">No such event.</response>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDetailDto>> GetEvent(int id)
    {
        var entity = await _db.Events.AsNoTracking()
            .Include(e => e.Venue)
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventId == id);
        return entity is null ? NotFound() : Ok(entity.ToDetailDto());
    }

    /// <summary>Creates a new event. Organisers only.</summary>
    /// <remarks>
    /// Expects { title, description, eventDate (yyyy-MM-dd, not in the past), eventType (Running | Walking | Cycling), venueId }.
    /// The logged-in Organiser becomes the event's owner.
    /// </remarks>
    /// <response code="201">The new event.</response>
    /// <response code="400">Validation failed, date in the past, or venue does not exist.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not an Organiser.</response>
    [HttpPost]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(EventDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EventDetailDto>> CreateEvent(EventRequest request)
    {
        var eventDate = request.EventDate!.Value;
        if (eventDate < DateOnly.FromDateTime(DateTime.Today))
        {
            ModelState.AddModelError(nameof(request.EventDate), "EventDate cannot be in the past.");
            return ValidationProblem(ModelState);
        }

        var venue = await _db.Venues.FirstOrDefaultAsync(v => v.VenueId == request.VenueId);
        if (venue is null)
        {
            ModelState.AddModelError(nameof(request.VenueId), "Venue does not exist.");
            return ValidationProblem(ModelState);
        }

        var entity = new RaceEvent
        {
            OrganiserId = User.GetUserId(),
            Venue = venue,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            EventDate = eventDate,
            EventType = request.EventType,
        };
        _db.Events.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEvent), new { id = entity.EventId }, entity.ToDetailDto());
    }

    /// <summary>Updates an event. Only the Organiser who owns it may do this.</summary>
    /// <param name="id">The event id.</param>
    /// <remarks>Expects the same body as event creation.</remarks>
    /// <response code="200">The updated event.</response>
    /// <response code="400">Validation failed or venue does not exist.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not an Organiser, or does not own this event.</response>
    /// <response code="404">No such event.</response>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(EventDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDetailDto>> UpdateEvent(int id, EventRequest request)
    {
        var entity = await _db.Events
            .Include(e => e.Venue)
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventId == id);
        if (entity is null) return NotFound();
        if (entity.OrganiserId != User.GetUserId()) return Forbid();

        var venue = await _db.Venues.FirstOrDefaultAsync(v => v.VenueId == request.VenueId);
        if (venue is null)
        {
            ModelState.AddModelError(nameof(request.VenueId), "Venue does not exist.");
            return ValidationProblem(ModelState);
        }

        entity.Title = request.Title.Trim();
        entity.Description = request.Description?.Trim();
        entity.EventDate = request.EventDate!.Value;
        entity.EventType = request.EventType;
        entity.Venue = venue;
        await _db.SaveChangesAsync();

        return Ok(entity.ToDetailDto());
    }

    /// <summary>Deletes an event (and its categories). Only the owning Organiser may do this.</summary>
    /// <param name="id">The event id.</param>
    /// <response code="204">Deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not an Organiser, or does not own this event.</response>
    /// <response code="404">No such event.</response>
    /// <response code="409">The event already has enrolments, so it cannot be deleted.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var entity = await _db.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (entity is null) return NotFound();
        if (entity.OrganiserId != User.GetUserId()) return Forbid();

        if (await _db.Enrolments.AnyAsync(en => en.Category.EventId == id))
            return Problem(title: "This event has enrolments and cannot be deleted.", statusCode: StatusCodes.Status409Conflict);

        _db.Events.Remove(entity); // categories are removed by the cascade
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
