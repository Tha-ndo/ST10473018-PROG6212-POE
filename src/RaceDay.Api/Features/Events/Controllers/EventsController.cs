using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Extensions;
using RaceDay.Api.Models;
using RaceDay.Api.Storage;

namespace RaceDay.Api.Controllers;

/// <summary>Browse events (public) and manage them (Organisers, owner only).</summary>
[ApiController]
[Route("api/events")]
[Produces("application/json")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IFileStorageService _storage;

    public EventsController(RaceDayDbContext db, IFileStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    /// <summary>Lists all upcoming events so anyone can browse without logging in. Supports optional filters.</summary>
    /// <param name="type">Running, Walking or Cycling.</param>
    /// <param name="province">Venue province, e.g. Gauteng.</param>
    /// <param name="search">Matches the title, description, venue name or city.</param>
    /// <param name="fromDate">Query "from": only events on or after this date (yyyy-MM-dd).</param>
    /// <param name="toDate">Query "to": only events on or before this date (yyyy-MM-dd).</param>
    /// <response code="200">Array of upcoming events (today onwards), soonest first.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<EventSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EventSummaryDto>>> GetUpcomingEvents(
        [FromQuery] string? type, [FromQuery] string? province, [FromQuery] string? search,
        [FromQuery(Name = "from")] DateOnly? fromDate, [FromQuery(Name = "to")] DateOnly? toDate)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var query = _db.Events.AsNoTracking().Where(e => e.EventDate >= today);

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(e => e.EventType == type.Trim());
        if (!string.IsNullOrWhiteSpace(province))
            query = query.Where(e => e.Venue.Province == province.Trim());
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(e => e.Title.ToLower().Contains(term)
                                     || (e.Description != null && e.Description.ToLower().Contains(term))
                                     || e.Venue.Name.ToLower().Contains(term)
                                     || e.Venue.City.ToLower().Contains(term));
        }
        if (fromDate is { } start) query = query.Where(e => e.EventDate >= start);
        if (toDate is { } end) query = query.Where(e => e.EventDate <= end);

        var events = await query
            .OrderBy(e => e.EventDate)
            .Select(e => new EventSummaryDto(
                e.EventId, e.Title, e.Description, e.EventDate, e.EventType,
                e.OrganiserId, e.VenueId, e.Venue.Name, e.Venue.City, e.Venue.Province, e.BannerImageUrl,
                e.Categories.Min(c => (decimal?)c.DistanceKm), e.Categories.Max(c => (decimal?)c.DistanceKm)))
            .ToListAsync();
        return Ok(events);
    }

    /// <summary>Lists every event (past and upcoming) owned by the logged-in Organiser, with enrolment totals.</summary>
    /// <response code="200">The Organiser's events, soonest first.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not an Organiser.</response>
    [HttpGet("mine")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(IEnumerable<OrganiserEventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<OrganiserEventDto>>> GetMyEvents()
    {
        var userId = User.GetUserId();
        var events = await _db.Events.AsNoTracking()
            .Where(e => e.OrganiserId == userId)
            .OrderBy(e => e.EventDate)
            .Select(e => new OrganiserEventDto(
                e.EventId, e.Title, e.EventDate, e.EventType, e.Venue.Name, e.Venue.City,
                e.Categories.Count(),
                e.Categories.SelectMany(c => c.Enrolments).Count(en => en.Status == EnrolmentStatus.Confirmed),
                e.Categories.Sum(c => c.MaxParticipants),
                e.Categories.SelectMany(c => c.Enrolments).Count(en => en.Result != null),
                e.BannerImageUrl))
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

        var bannerUrl = entity.BannerImageUrl;
        _db.Events.Remove(entity); // categories are removed by the cascade
        await _db.SaveChangesAsync();
        await _storage.DeleteAsync(bannerUrl);
        return NoContent();
    }

    /// <summary>Uploads (or replaces) the event's banner image in Azure Blob Storage. Owner only.</summary>
    /// <param name="id">The event id.</param>
    /// <param name="request">multipart/form-data with a single image field named "file" (JPG, PNG, WEBP or GIF, max 5 MB).</param>
    /// <response code="200">The updated event, including bannerImageUrl.</response>
    /// <response code="400">No file, wrong type or too large.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not an Organiser, or does not own this event.</response>
    /// <response code="404">No such event.</response>
    [HttpPost("{id:int}/banner")]
    [Authorize(Roles = Roles.Organiser)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ImageUploadRules.MaxBytes + 64 * 1024)]
    [ProducesResponseType(typeof(EventDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDetailDto>> UploadBanner(int id, [FromForm] ImageUploadRequest request, CancellationToken ct)
    {
        var entity = await _db.Events
            .Include(e => e.Venue)
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventId == id, ct);
        if (entity is null) return NotFound();
        if (entity.OrganiserId != User.GetUserId()) return Forbid();

        var error = ImageUploadRules.Validate(request.File, out var extension, out var contentType);
        if (error is not null)
        {
            ModelState.AddModelError("File", error);
            return ValidationProblem(ModelState);
        }

        var oldUrl = entity.BannerImageUrl;
        await using (var stream = request.File!.OpenReadStream())
        {
            entity.BannerImageUrl = await _storage.UploadAsync(stream, contentType, "event-banners", extension, ct);
        }
        await _db.SaveChangesAsync(ct);
        await _storage.DeleteAsync(oldUrl, ct);

        return Ok(entity.ToDetailDto());
    }

    /// <summary>Removes the event's banner image. Owner only.</summary>
    /// <param name="id">The event id.</param>
    /// <response code="204">Removed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not an Organiser, or does not own this event.</response>
    /// <response code="404">No such event.</response>
    [HttpDelete("{id:int}/banner")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBanner(int id, CancellationToken ct)
    {
        var entity = await _db.Events.FirstOrDefaultAsync(e => e.EventId == id, ct);
        if (entity is null) return NotFound();
        if (entity.OrganiserId != User.GetUserId()) return Forbid();

        var oldUrl = entity.BannerImageUrl;
        entity.BannerImageUrl = null;
        await _db.SaveChangesAsync(ct);
        await _storage.DeleteAsync(oldUrl, ct);
        return NoContent();
    }
}
