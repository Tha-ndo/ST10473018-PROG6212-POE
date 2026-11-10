using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Profile page for both roles: view/update details and upload a profile picture (stored in Azure via the API).</summary>
[RequireLogin]
public class ProfileController : Controller
{
    private readonly RaceDayApiClient _api;
    private readonly IUserSession _session;

    public ProfileController(RaceDayApiClient api, IUserSession session)
    {
        _api = api;
        _session = session;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var me = await _api.GetProfileAsync();
        if (!me.Success || me.Data is null)
        {
            TempData.Error(me.Error ?? "We couldn't load your profile.");
            return RedirectToAction("Index", "Home");
        }
        _session.UpdateProfile(me.Data);
        return View(ToViewModel(me.Data));
    }

    [HttpPost]
    public async Task<IActionResult> Index(ProfileViewModel model)
    {
        if (!ModelState.IsValid) return await RedisplayAsync(model);

        var result = await _api.UpdateProfileAsync(new UpdateProfileRequest(
            model.FullName.Trim(), string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim()));

        if (!result.Success)
        {
            ModelState.AddApiErrors(result, typeof(ProfileViewModel));
            return await RedisplayAsync(model);
        }

        _session.UpdateProfile(result.Data!);
        TempData.Success("Your details have been saved.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Photo(IFormFile? photo)
    {
        if (photo is null || photo.Length == 0)
        {
            TempData.Error("Choose an image to upload.");
            return RedirectToAction(nameof(Index));
        }

        var result = await _api.UploadProfilePictureAsync(photo);
        if (result.Success)
        {
            _session.UpdateProfile(result.Data!);
            TempData.Success("Looking sharp! Your profile picture has been updated.");
        }
        else
        {
            TempData.Error(result.FieldErrors.Values.SelectMany(v => v).FirstOrDefault() ?? result.Error ?? "Upload failed.");
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> RedisplayAsync(ProfileViewModel model)
    {
        var me = await _api.GetProfileAsync();
        if (me.Data is not null)
        {
            model.Email = me.Data.Email;
            model.Role = me.Data.Role;
            model.CreatedAt = me.Data.CreatedAt;
            model.ProfilePictureUrl = me.Data.ProfilePictureUrl;
        }
        return View(nameof(Index), model);
    }

    private static ProfileViewModel ToViewModel(UserDto u) => new()
    {
        FullName = u.FullName,
        PhoneNumber = u.PhoneNumber,
        Email = u.Email,
        Role = u.Role,
        CreatedAt = u.CreatedAt,
        ProfilePictureUrl = u.ProfilePictureUrl,
    };
}
