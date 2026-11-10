using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Participant-only pages: enrol, my enrolments, my results.</summary>
[RequireLogin(RoleNames.Participant)]
public class ParticipantController : Controller
{
    private readonly RaceDayApiClient _api;

    public ParticipantController(RaceDayApiClient api) => _api = api;

    [HttpPost]
    public async Task<IActionResult> Enrol(int eventId, int? categoryId)
    {
        if (categoryId is null)
        {
            TempData.Error("Choose a category to enrol in.");
            return RedirectToAction("Details", "Events", new { id = eventId });
        }

        var result = await _api.EnrolAsync(categoryId.Value);
        if (result.Success)
            TempData.Success($"You're in! Your enrolment is confirmed{(result.Data?.BibNumber is { } bib ? $" — race number {bib}" : "")}.");
        else
            TempData.Error(result.Error ?? "We couldn't enrol you.");

        return RedirectToAction("Details", "Events", new { id = eventId });
    }

    [HttpGet]
    public async Task<IActionResult> Enrolments()
    {
        var result = await _api.GetMyEnrolmentsAsync();
        var list = result.Data ?? new();
        var today = DateOnly.FromDateTime(DateTime.Today);

        return View(new MyEnrolmentsViewModel
        {
            Upcoming = list.Where(e => e.Status != "Cancelled" && e.EventDate >= today).OrderBy(e => e.EventDate).ToList(),
            Past = list.Where(e => e.Status != "Cancelled" && e.EventDate < today).OrderByDescending(e => e.EventDate).ToList(),
            Cancelled = list.Where(e => e.Status == "Cancelled").OrderByDescending(e => e.EventDate).ToList(),
        });
    }

    [HttpPost]
    public async Task<IActionResult> CancelEnrolment(int id, string? returnUrl = null)
    {
        var result = await _api.CancelEnrolmentAsync(id);
        if (result.Success) TempData.Success("Your enrolment has been cancelled.");
        else TempData.Error(result.Error ?? "We couldn't cancel that enrolment.");

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);
        return RedirectToAction(nameof(Enrolments));
    }

    [HttpGet]
    public async Task<IActionResult> Results()
    {
        var result = await _api.GetMyResultsAsync();
        return View(new MyResultsViewModel
        {
            Results = (result.Data ?? new()).OrderByDescending(r => r.EventDate).ToList(),
        });
    }
}
