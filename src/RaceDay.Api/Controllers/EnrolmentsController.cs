using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Extensions;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
[Produces("application/json")]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EnrolmentsController(RaceDayDbContext db) => _db = db;

    [HttpPost("categories/{categoryId:int}/enrolments")]
    [Authorize(Roles = Roles.Participant)]
    [ProducesResponseType(typeof(EnrolResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnrolResponse>> Enrol(int categoryId)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == categoryId);
        if (category is null) return NotFound();

        if (category.Event.EventDate < DateOnly.FromDateTime(DateTime.Today))
            return Problem(title: "This event has already taken place.", statusCode: StatusCodes.Status409Conflict);

        var userId = User.GetUserId();
        var existing = await _db.Enrolments.FirstOrDefaultAsync(e => e.ParticipantId == userId && e.CategoryId == categoryId);
        if (existing is { Status: EnrolmentStatus.Confirmed })
            return Problem(title: "You are already enrolled in this category.", statusCode: StatusCodes.Status409Conflict);

        var confirmed = await _db.Enrolments.CountAsync(e => e.CategoryId == categoryId && e.Status == EnrolmentStatus.Confirmed);
        if (confirmed >= category.MaxParticipants)
            return Problem(title: "This category is full.", statusCode: StatusCodes.Status409Conflict);

        Enrolment enrolment;
        if (existing is not null)
        {
            existing.Status = EnrolmentStatus.Confirmed;
            existing.EnrolmentDate = DateTime.Now;
            enrolment = existing;
        }
        else
        {
            var issued = await _db.Enrolments.CountAsync(e => e.Category.EventId == category.EventId);
            enrolment = new Enrolment
            {
                ParticipantId = userId,
                CategoryId = categoryId,
                BibNumber = (issued + 1).ToString("D4"),
                Status = EnrolmentStatus.Confirmed,
            };
            _db.Enrolments.Add(enrolment);
        }

        await _db.SaveChangesAsync();
        return StatusCode(StatusCodes.Status201Created, new EnrolResponse(enrolment.EnrolmentId, enrolment.BibNumber));
    }

    [HttpGet("enrolments/my")]
    [Authorize(Roles = Roles.Participant)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<EnrolmentDto>>> GetMyEnrolments()
    {
        var userId = User.GetUserId();
        var list = await _db.Enrolments.AsNoTracking()
            .Where(e => e.ParticipantId == userId)
            .OrderByDescending(e => e.EnrolmentDate)
            .Select(Mappers.EnrolmentProjection)
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("events/{eventId:int}/enrolments")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<EnrolmentDto>>> GetEventEnrolments(int eventId)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != User.GetUserId()) return Forbid();

        var list = await _db.Enrolments.AsNoTracking()
            .Where(e => e.Category.EventId == eventId)
            .OrderBy(e => e.BibNumber)
            .Select(Mappers.EnrolmentProjection)
            .ToListAsync();
        return Ok(list);
    }

    [HttpDelete("enrolments/{id:int}")]
    [Authorize(Roles = Roles.Participant)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelEnrolment(int id)
    {
        var enrolment = await _db.Enrolments.FirstOrDefaultAsync(e => e.EnrolmentId == id);
        if (enrolment is null) return NotFound();
        if (enrolment.ParticipantId != User.GetUserId()) return Forbid();

        enrolment.Status = EnrolmentStatus.Cancelled;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
