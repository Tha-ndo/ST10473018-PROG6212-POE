using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Extensions;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Race-day results. Organisers capture them for their own events; Participants view their own.</summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public ResultsController(RaceDayDbContext db) => _db = db;

    /// <summary>Captures a result (finish time and position) against an enrolment. Owner of the parent event only.</summary>
    /// <param name="enrolmentId">The enrolment the result belongs to.</param>
    /// <remarks>Expects { finishTime (hh:mm:ss), position, status (Finished | DNF | DNS) }.
    /// finishTime and position are required when status is "Finished".</remarks>
    /// <response code="201">The new result.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not an Organiser, or does not own the parent event.</response>
    /// <response code="404">No such enrolment.</response>
    /// <response code="409">The enrolment was cancelled, or already has a result.</response>
    [HttpPost("enrolments/{enrolmentId:int}/results")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResultDto>> CaptureResult(int enrolmentId, ResultRequest request)
    {
        var enrolment = await _db.Enrolments
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentId == enrolmentId);
        if (enrolment is null) return NotFound();
        if (enrolment.Category.Event.OrganiserId != User.GetUserId()) return Forbid();

        if (!IsResultValid(request)) return ValidationProblem(ModelState);

        if (enrolment.Status != EnrolmentStatus.Confirmed)
            return Problem(title: "Results cannot be captured for a cancelled enrolment.", statusCode: StatusCodes.Status409Conflict);
        if (await _db.Results.AnyAsync(r => r.EnrolmentId == enrolmentId))
            return Problem(title: "A result has already been captured for this enrolment. Use PUT /api/results/{id} to correct it.", statusCode: StatusCodes.Status409Conflict);

        var result = new RaceResult
        {
            EnrolmentId = enrolmentId,
            FinishTime = request.FinishTime,
            Position = request.Position,
            Status = request.Status,
        };
        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        var dto = await _db.Results.AsNoTracking()
            .Where(r => r.ResultId == result.ResultId)
            .Select(Mappers.ResultProjection)
            .FirstAsync();
        return StatusCode(StatusCodes.Status201Created, dto);
    }

    /// <summary>Corrects a captured result. Owner of the parent event only.</summary>
    /// <param name="id">The result id.</param>
    /// <remarks>Expects { finishTime, position, status }, same rules as capturing a result.</remarks>
    /// <response code="200">The updated result.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not an Organiser, or does not own the parent event.</response>
    /// <response code="404">No such result.</response>
    [HttpPut("results/{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultDto>> UpdateResult(int id, ResultRequest request)
    {
        var result = await _db.Results
            .Include(r => r.Enrolment).ThenInclude(e => e.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(r => r.ResultId == id);
        if (result is null) return NotFound();
        if (result.Enrolment.Category.Event.OrganiserId != User.GetUserId()) return Forbid();

        if (!IsResultValid(request)) return ValidationProblem(ModelState);

        result.FinishTime = request.FinishTime;
        result.Position = request.Position;
        result.Status = request.Status;
        await _db.SaveChangesAsync();

        var dto = await _db.Results.AsNoTracking()
            .Where(r => r.ResultId == id)
            .Select(Mappers.ResultProjection)
            .FirstAsync();
        return Ok(dto);
    }

    /// <summary>Returns the logged-in Participant's personal results across all events.</summary>
    /// <response code="200">Array of results.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not a Participant.</response>
    [HttpGet("results/my")]
    [Authorize(Roles = Roles.Participant)]
    [ProducesResponseType(typeof(IEnumerable<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<ResultDto>>> GetMyResults()
    {
        var userId = User.GetUserId();
        var list = await _db.Results.AsNoTracking()
            .Where(r => r.Enrolment.ParticipantId == userId)
            .Select(Mappers.ResultProjection)
            .ToListAsync();
        return Ok(list);
    }

    /// <summary>Returns the public results list (leaderboard) for an event, best position first.</summary>
    /// <param name="eventId">The event id.</param>
    /// <response code="200">Array of results.</response>
    /// <response code="404">No such event.</response>
    [HttpGet("events/{eventId:int}/results")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ResultDto>>> GetEventResults(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId)) return NotFound();

        var list = await _db.Results.AsNoTracking()
            .Where(r => r.Enrolment.Category.EventId == eventId)
            .OrderBy(r => r.Position == null)
            .ThenBy(r => r.Position)
            .Select(Mappers.ResultProjection)
            .ToListAsync();
        return Ok(list);
    }

    private bool IsResultValid(ResultRequest request)
    {
        if (request.Status == ResultStatus.Finished)
        {
            if (request.FinishTime is null)
                ModelState.AddModelError(nameof(request.FinishTime), "FinishTime is required when Status is 'Finished'.");
            if (request.Position is null)
                ModelState.AddModelError(nameof(request.Position), "Position is required when Status is 'Finished'.");
        }
        if (request.FinishTime is { } t && t <= TimeSpan.Zero)
            ModelState.AddModelError(nameof(request.FinishTime), "FinishTime must be greater than zero.");

        return ModelState.IsValid;
    }
}
