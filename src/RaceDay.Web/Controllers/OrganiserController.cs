using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>
/// Organiser-only interface: dashboard, events, categories, enrolments, results and venues.
/// Every change is sent to the API, which owns the rules (ownership, dates, capacity...). This controller
/// only gathers form input, calls the API and shows the outcome.
/// </summary>
[RequireLogin(RoleNames.Organiser)]
public class OrganiserController : Controller
{
    private readonly RaceDayApiClient _api;
    private readonly IUserSession _session;

    public OrganiserController(RaceDayApiClient api, IUserSession session)
    {
        _api = api;
        _session = session;
    }

    // ================= Dashboard =================

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var events = await _api.GetMyEventsAsync();
        if (!events.Success) TempData.Error(events.Error ?? "We couldn't load your events.");
        return View(new DashboardViewModel { Events = events.Data ?? new() });
    }

    // ================= Events =================

    [HttpGet]
    public async Task<IActionResult> CreateEvent()
    {
        var model = new EventFormViewModel
        {
            EventDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(2)),
            FirstCategoryEntryFee = 0,
            FirstCategoryMaxParticipants = 500,
        };
        await LoadVenuesAsync(model);
        return View("EventForm", model);
    }

    [HttpPost]
    public async Task<IActionResult> CreateEvent(EventFormViewModel model)
    {
        model.EventId = null;
        if (model.FirstCategoryDistanceKm is null)
            ModelState.AddModelError(nameof(model.FirstCategoryDistanceKm), "Enter the race distance for the first category.");

        if (!ModelState.IsValid)
        {
            await LoadVenuesAsync(model);
            return View("EventForm", model);
        }

        var created = await _api.CreateEventAsync(ToRequest(model));
        if (!created.Success)
        {
            ModelState.AddApiErrors(created, typeof(EventFormViewModel));
            await LoadVenuesAsync(model);
            return View("EventForm", model);
        }

        var eventId = created.Data!.EventId;
        var warnings = new List<string>();

        var distance = model.FirstCategoryDistanceKm!.Value;
        var category = await _api.CreateCategoryAsync(eventId, new CategoryRequest(
            string.IsNullOrWhiteSpace(model.FirstCategoryName) ? distance.Km() : model.FirstCategoryName.Trim(),
            distance, model.FirstCategoryEntryFee ?? 0, model.FirstCategoryMaxParticipants ?? 100));
        if (!category.Success) warnings.Add("the first category could not be saved (" + FirstError(category) + ")");

        if (model.Banner is { Length: > 0 })
        {
            var banner = await _api.UploadBannerAsync(eventId, model.Banner);
            if (!banner.Success) warnings.Add("the banner image was not uploaded (" + FirstError(banner) + ")");
        }

        if (warnings.Count > 0) TempData.Warning($"Event created, but {string.Join(" and ", warnings)}.");
        else TempData.Success($"\"{created.Data.Title}\" is live! Add more distance or age categories below.");

        return RedirectToAction(nameof(Categories), new { id = eventId });
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var ev = await GetOwnedEventAsync(id);
        if (ev is null) return OwnershipFailure();

        var model = new EventFormViewModel
        {
            EventId = ev.EventId,
            Title = ev.Title,
            Description = ev.Description,
            EventDate = ev.EventDate,
            EventType = ev.EventType,
            VenueId = ev.Venue.VenueId,
            ExistingBannerUrl = ev.BannerImageUrl,
        };
        await LoadVenuesAsync(model);
        ViewData["Categories"] = ev.Categories;
        return View("EventForm", model);
    }

    [HttpPost]
    public async Task<IActionResult> EditEvent(int id, EventFormViewModel model)
    {
        model.EventId = id;
        if (!ModelState.IsValid) return await RedisplayEditAsync(id, model);

        var updated = await _api.UpdateEventAsync(id, ToRequest(model));
        if (!updated.Success)
        {
            ModelState.AddApiErrors(updated, typeof(EventFormViewModel));
            return await RedisplayEditAsync(id, model);
        }

        if (model.Banner is { Length: > 0 })
        {
            var banner = await _api.UploadBannerAsync(id, model.Banner);
            if (!banner.Success)
            {
                TempData.Warning("Event details saved, but the banner was not uploaded: " + FirstError(banner));
                return RedirectToAction(nameof(EditEvent), new { id });
            }
        }

        TempData.Success("Event updated.");
        return RedirectToAction(nameof(Dashboard));
    }

    [HttpPost]
    public async Task<IActionResult> RemoveBanner(int id)
    {
        var result = await _api.DeleteBannerAsync(id);
        if (result.Success) TempData.Success("Banner removed.");
        else TempData.Error(result.Error ?? "The banner could not be removed.");
        return RedirectToAction(nameof(EditEvent), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var result = await _api.DeleteEventAsync(id);
        if (result.Success) TempData.Success("Event deleted.");
        else TempData.Error(result.Error ?? "The event could not be deleted.");
        return RedirectToAction(nameof(Dashboard));
    }

    // ================= Categories =================

    [HttpGet]
    public async Task<IActionResult> Categories(int id)
    {
        var ev = await GetOwnedEventAsync(id);
        if (ev is null) return OwnershipFailure();
        return View(await BuildCategoriesPageAsync(ev, new CategoryFormViewModel { EventId = id }));
    }

    [HttpPost]
    public async Task<IActionResult> AddCategory(CategoryFormViewModel model)
    {
        if (!ModelState.IsValid) return await RedisplayCategoriesAsync(model);

        var result = await _api.CreateCategoryAsync(model.EventId, ToRequest(model));
        if (!result.Success)
        {
            ModelState.AddApiErrors(result, typeof(CategoryFormViewModel), prefix: "NewCategory.");
            return await RedisplayCategoriesAsync(model);
        }

        TempData.Success($"Category \"{result.Data!.Name}\" added.");
        return RedirectToAction(nameof(Categories), new { id = model.EventId });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateCategory(CategoryFormViewModel model)
    {
        if (model.CategoryId is null) return BadRequest();
        if (!ModelState.IsValid)
        {
            TempData.Error(string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return RedirectToAction(nameof(Categories), new { id = model.EventId });
        }

        var result = await _api.UpdateCategoryAsync(model.CategoryId.Value, ToRequest(model));
        if (result.Success) TempData.Success($"Category \"{result.Data!.Name}\" updated.");
        else TempData.Error(FirstError(result));
        return RedirectToAction(nameof(Categories), new { id = model.EventId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCategory(int id, int eventId)
    {
        var result = await _api.DeleteCategoryAsync(id);
        if (result.Success) TempData.Success("Category removed.");
        else TempData.Error(result.Error ?? "The category could not be removed.");
        return RedirectToAction(nameof(Categories), new { id = eventId });
    }

    // ================= Enrolments =================

    [HttpGet]
    public async Task<IActionResult> Enrolments(int id, int? categoryId, string? status, string? search)
    {
        var ev = await GetOwnedEventAsync(id);
        if (ev is null) return OwnershipFailure();

        var result = await _api.GetEventEnrolmentsAsync(id);
        if (!result.Success) TempData.Error(result.Error ?? "We couldn't load the enrolments.");
        var all = result.Data ?? new();

        // Narrowing an already-fetched list for display only.
        IEnumerable<EnrolmentDto> shown = all;
        if (categoryId is not null) shown = shown.Where(e => e.CategoryId == categoryId);
        if (!string.IsNullOrWhiteSpace(status)) shown = shown.Where(e => e.Status == status);
        if (!string.IsNullOrWhiteSpace(search))
            shown = shown.Where(e => e.ParticipantName.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase)
                                     || (e.BibNumber ?? "").Contains(search.Trim(), StringComparison.OrdinalIgnoreCase));

        return View(new EnrolmentsPageViewModel
        {
            Event = ev,
            Enrolments = shown.ToList(),
            CategoryId = categoryId,
            Status = status,
            Search = search,
            TotalConfirmed = all.Count(e => e.Status == "Confirmed"),
            TotalCancelled = all.Count(e => e.Status == "Cancelled"),
        });
    }

    // ================= Results =================

    [HttpGet]
    public async Task<IActionResult> Results(int id, int? categoryId)
    {
        var ev = await GetOwnedEventAsync(id);
        if (ev is null) return OwnershipFailure();

        var enrolments = await _api.GetEventEnrolmentsAsync(id);
        var results = await _api.GetEventResultsAsync(id);
        var byEnrolment = (results.Data ?? new()).ToDictionary(r => r.EnrolmentId);

        var rows = (enrolments.Data ?? new())
            .Where(e => e.Status == "Confirmed" || byEnrolment.ContainsKey(e.EnrolmentId))
            .Where(e => categoryId is null || e.CategoryId == categoryId)
            .Select(e => new ResultRowViewModel { Enrolment = e, Result = byEnrolment.GetValueOrDefault(e.EnrolmentId) })
            .OrderBy(r => r.Result?.Position ?? int.MaxValue)
            .ThenBy(r => r.Enrolment.BibNumber)
            .ToList();

        return View(new ResultsEntryViewModel { Event = ev, Rows = rows, CategoryId = categoryId });
    }

    [HttpPost]
    public async Task<IActionResult> SaveResult(ResultFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData.Error(string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return RedirectToResults(model);
        }

        TimeSpan? finish = null;
        if (!string.IsNullOrWhiteSpace(model.FinishTime))
        {
            var parts = model.FinishTime.Split(':').Select(p => int.Parse(p, CultureInfo.InvariantCulture)).ToArray();
            finish = new TimeSpan(parts[0], parts[1], parts[2]);
        }

        var request = new ResultRequest(finish, model.Position, model.Status);
        var result = model.ResultId is { } resultId
            ? await _api.UpdateResultAsync(resultId, request)
            : await _api.CaptureResultAsync(model.EnrolmentId, request);

        if (result.Success)
        {
            var r = result.Data!;
            TempData.Success(r.Status == "Finished"
                ? $"Result published: {r.ParticipantName} — {r.Position!.Value.Ordinal()} in {((TimeSpan?)r.FinishTime).ToRaceTime()}."
                : $"Result published: {r.ParticipantName} — {r.Status}.");
        }
        else
        {
            TempData.Error(FirstError(result));
        }
        return RedirectToResults(model);
    }

    // ================= Venues =================

    [HttpGet]
    public IActionResult NewVenue(string? returnUrl = null) => View(new VenueFormViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    public async Task<IActionResult> NewVenue(VenueFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _api.CreateVenueAsync(new VenueRequest(
            model.Name.Trim(), model.AddressLine?.Trim(), model.City.Trim(), model.Province?.Trim(), model.Latitude, model.Longitude));
        if (!result.Success)
        {
            ModelState.AddApiErrors(result, typeof(VenueFormViewModel));
            return View(model);
        }

        TempData.Success($"Venue \"{result.Data!.Name}\" added. You can now pick it for your event.");
        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)) return LocalRedirect(model.ReturnUrl);
        return RedirectToAction(nameof(CreateEvent));
    }

    // ================= Helpers =================

    /// <summary>Loads an event and checks it belongs to the logged-in Organiser (the API enforces this too on every change).</summary>
    private async Task<EventDetailDto?> GetOwnedEventAsync(int id)
    {
        var ev = await _api.GetEventAsync(id);
        if (!ev.Success || ev.Data is null) return null;
        return ev.Data.OrganiserId == _session.UserId ? ev.Data : null;
    }

    private IActionResult OwnershipFailure()
    {
        TempData.Error("That event doesn't exist or isn't one of yours.");
        return RedirectToAction(nameof(Dashboard));
    }

    private async Task LoadVenuesAsync(EventFormViewModel model)
    {
        var venues = await _api.GetVenuesAsync();
        model.Venues = venues.Data ?? new();
    }

    private async Task<IActionResult> RedisplayEditAsync(int id, EventFormViewModel model)
    {
        var ev = await GetOwnedEventAsync(id);
        if (ev is null) return OwnershipFailure();
        model.ExistingBannerUrl = ev.BannerImageUrl;
        ViewData["Categories"] = ev.Categories;
        await LoadVenuesAsync(model);
        return View("EventForm", model);
    }

    private async Task<CategoriesPageViewModel> BuildCategoriesPageAsync(EventDetailDto ev, CategoryFormViewModel form)
    {
        var enrolments = await _api.GetEventEnrolmentsAsync(ev.EventId);
        return new CategoriesPageViewModel
        {
            Event = ev,
            NewCategory = form,
            ConfirmedPerCategory = (enrolments.Data ?? new())
                .Where(e => e.Status == "Confirmed")
                .GroupBy(e => e.CategoryId)
                .ToDictionary(g => g.Key, g => g.Count()),
        };
    }

    private async Task<IActionResult> RedisplayCategoriesAsync(CategoryFormViewModel model)
    {
        var ev = await GetOwnedEventAsync(model.EventId);
        if (ev is null) return OwnershipFailure();

        // Errors were recorded against the posted field names; move them under the NewCategory prefix used by the view.
        foreach (var key in ModelState.Keys.Where(k => !string.IsNullOrEmpty(k) && !k.StartsWith("NewCategory.")).ToList())
        {
            foreach (var error in ModelState[key]!.Errors) ModelState.AddModelError("NewCategory." + key, error.ErrorMessage);
        }
        return View(nameof(Categories), await BuildCategoriesPageAsync(ev, model));
    }

    private IActionResult RedirectToResults(ResultFormViewModel model) =>
        Redirect(Url.Action(nameof(Results), new { id = model.EventId, categoryId = model.CategoryId }) + $"#row-{model.EnrolmentId}");

    private static EventRequest ToRequest(EventFormViewModel m) =>
        new(m.Title.Trim(), string.IsNullOrWhiteSpace(m.Description) ? null : m.Description.Trim(), m.EventDate!.Value, m.EventType, m.VenueId ?? 0);

    private static CategoryRequest ToRequest(CategoryFormViewModel m) =>
        new(m.Name.Trim(), m.DistanceKm ?? 0, m.EntryFee ?? 0, m.MaxParticipants ?? 0);

    private static string FirstError(ApiResult result) =>
        result.FieldErrors.Values.SelectMany(v => v).FirstOrDefault() ?? result.Error ?? "Unknown error";
}
