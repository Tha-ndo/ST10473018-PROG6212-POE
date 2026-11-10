using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : Controller
{
    private readonly RaceDayApiClient _api;
    private readonly IUserSession _session;
    private readonly ILogger<HomeController> _logger;

    public HomeController(RaceDayApiClient api, IUserSession session, ILogger<HomeController> logger)
    {
        _api = api;
        _session = session;
        _logger = logger;
    }

    /// <summary>Participant (and visitor) home page: all upcoming events with filters. Filtering is done by the API.</summary>
    [HttpGet]
    public async Task<IActionResult> Index(EventBrowseViewModel filters)
    {
        if (_session.IsOrganiser) return RedirectToAction("Dashboard", "Organiser");

        var events = await _api.GetUpcomingEventsAsync(filters.Type, filters.Province, filters.Search, filters.From, filters.To);
        var venues = await _api.GetVenuesAsync();

        filters.Events = events.Data ?? new();
        filters.Provinces = (venues.Data ?? new())
            .Select(v => v.Province).Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p!)
            .Distinct().OrderBy(p => p).ToList();

        if (_session.IsParticipant)
        {
            var mine = await _api.GetMyEnrolmentsAsync();
            filters.EnrolledEventIds = (mine.Data ?? new()).Where(e => e.Status == "Confirmed").Select(e => e.EventId).ToHashSet();
        }

        if (!events.Success) ViewData["LoadError"] = events.Error;
        return View(filters);
    }

    [HttpGet]
    public IActionResult Error()
    {
        var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        if (feature is not null) _logger.LogError(feature.Error, "Unhandled error on {Path}", feature.Path);
        return View();
    }

    [HttpGet("/Home/Status/{code:int}")]
    public IActionResult Status(int code)
    {
        ViewData["StatusCode"] = code;
        return View(code == 404 ? "NotFound" : "Error");
    }
}
