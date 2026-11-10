using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Extensions;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>
/// Supporting endpoints (an addition to the Part 1 plan): events reference a venue, so clients
/// need a way to list venues and Organisers need a way to add one.
/// </summary>
[ApiController]
[Route("api/venues")]
[Produces("application/json")]
public class VenuesController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public VenuesController(RaceDayDbContext db) => _db = db;

    /// <summary>Lists all venues so a venueId can be chosen when creating an event.</summary>
    /// <response code="200">Array of venues.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<VenueDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<VenueDto>>> GetVenues()
    {
        var venues = await _db.Venues.AsNoTracking().OrderBy(v => v.Name).ToListAsync();
        return Ok(venues.Select(v => v.ToDto()));
    }

    /// <summary>Creates a new venue. Organisers only.</summary>
    /// <remarks>Expects { name, addressLine, city, province, latitude, longitude }.</remarks>
    /// <response code="201">The new venue.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not an Organiser.</response>
    [HttpPost]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(VenueDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<VenueDto>> CreateVenue(VenueRequest request)
    {
        var venue = new Venue
        {
            Name = request.Name.Trim(),
            AddressLine = request.AddressLine?.Trim(),
            City = request.City.Trim(),
            Province = request.Province?.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
        };
        _db.Venues.Add(venue);
        await _db.SaveChangesAsync();
        return StatusCode(StatusCodes.Status201Created, venue.ToDto());
    }
}
