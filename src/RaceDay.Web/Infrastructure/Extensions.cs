using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using RaceDay.Web.Services;

namespace RaceDay.Web.Infrastructure;

public static class FlashExtensions
{
    public const string SuccessKey = "flash.success";
    public const string WarningKey = "flash.warning";
    public const string ErrorKey = "flash.error";

    public static void Success(this ITempDataDictionary tempData, string message) => tempData[SuccessKey] = message;
    public static void Warning(this ITempDataDictionary tempData, string message) => tempData[WarningKey] = message;
    public static void Error(this ITempDataDictionary tempData, string message) => tempData[ErrorKey] = message;
}

public static class ModelStateExtensions
{
    /// <summary>
    /// Copies the API's validation errors onto the form: errors for fields that exist on the view model
    /// show next to that field, everything else goes to the validation summary.
    /// </summary>
    public static void AddApiErrors(this ModelStateDictionary modelState, ApiResult result, Type viewModel, string prefix = "")
    {
        var props = viewModel.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var (field, messages) in result.FieldErrors)
        {
            var match = props.FirstOrDefault(p => string.Equals(p, field, StringComparison.OrdinalIgnoreCase));
            var key = match is null ? string.Empty : prefix + match;
            foreach (var message in messages) modelState.AddModelError(key, message);
        }

        if (result.FieldErrors.Count == 0 && !string.IsNullOrEmpty(result.Error))
            modelState.AddModelError(string.Empty, result.Error);
    }
}

/// <summary>Single source of colour coding for every status badge so pages stay consistent.</summary>
public static class StatusStyles
{
    public static string BadgeClass(string? status) => status switch
    {
        "Confirmed" or "Finished" or "Published" => "badge-status badge-status--success",
        "Pending" or "DNS" or "Awaiting result" => "badge-status badge-status--warning",
        "Cancelled" or "DNF" => "badge-status badge-status--danger",
        "Upcoming" or "Open" => "badge-status badge-status--info",
        _ => "badge-status badge-status--muted",
    };

    public static string Icon(string? status) => status switch
    {
        "Confirmed" or "Finished" or "Published" => "bi-check-circle-fill",
        "Pending" or "Awaiting result" => "bi-hourglass-split",
        "DNS" => "bi-dash-circle-fill",
        "Cancelled" or "DNF" => "bi-x-circle-fill",
        "Upcoming" or "Open" => "bi-calendar-event",
        _ => "bi-circle",
    };

    public static string EventTypeIcon(string? type) => type switch
    {
        "Cycling" => "bi-bicycle",
        "Walking" => "bi-person-walking",
        _ => "bi-lightning-charge-fill",
    };

    public static string EventTypeClass(string? type) => type switch
    {
        "Cycling" => "type-chip type-chip--cycling",
        "Walking" => "type-chip type-chip--walking",
        _ => "type-chip type-chip--running",
    };
}

public static class FormatExtensions
{
    public static string Ordinal(this int n)
    {
        var suffix = (n % 100) is 11 or 12 or 13 ? "th" : (n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
        return $"{n}{suffix}";
    }

    public static string ToRaceTime(this TimeSpan? time) =>
        time is null ? "—" : $"{(int)time.Value.TotalHours:00}:{time.Value.Minutes:00}:{time.Value.Seconds:00}";

    public static string Km(this decimal km) => $"{km.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)} km";

    public static string Rand(this decimal amount) =>
        amount == 0 ? "Free" : "R " + amount.ToString("#,##0.00", System.Globalization.CultureInfo.InvariantCulture).Replace(",", " ");
}
