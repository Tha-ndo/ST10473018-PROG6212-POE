using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Public event detail page (banner, venue, categories, enrol form and leaderboard).</summary>
public class EventsController : Controller
{
    private readonly RaceDayApiClient _api;
    private readonly IUserSession _session;

    public EventsController(RaceDayApiClient api, IUserSession session)
    {
        _api = api;
        _session = session;
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var ev = await _api.GetEventAsync(id);
        if (!ev.Success || ev.Data is null) return NotFound();

        var results = await _api.GetEventResultsAsync(id);
        var model = new EventDetailsViewModel { Event = ev.Data, Results = results.Data ?? new() };

        if (_session.IsParticipant)
        {
            var mine = await _api.GetMyEnrolmentsAsync();
            model.MyEnrolments = (mine.Data ?? new()).Where(e => e.EventId == id).ToList();
        }

        return View(model);
    }
}
