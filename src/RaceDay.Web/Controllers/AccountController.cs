using System.Net;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Register, log in and log out. Credentials are checked by the API; the session keeps the result.</summary>
public class AccountController : Controller
{
    private readonly RaceDayApiClient _api;
    private readonly IUserSession _session;

    public AccountController(RaceDayApiClient api, IUserSession session)
    {
        _api = api;
        _session = session;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (_session.IsAuthenticated) return RedirectToRoleHome();
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var login = await _api.LoginAsync(new LoginRequest(model.Email.Trim(), model.Password));
        if (!login.Success)
        {
            ModelState.AddModelError(string.Empty, login.StatusCode == HttpStatusCode.Unauthorized
                ? "That email and password combination is not correct."
                : login.Error ?? "Login failed.");
            model.Password = string.Empty;
            return View(model);
        }

        if (!await StartSessionAsync(login.Data!))
        {
            ModelState.AddModelError(string.Empty, "We couldn't load your profile. Please try again.");
            return View(model);
        }

        TempData.Success($"Welcome back, {FirstName()}!");
        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)) return LocalRedirect(model.ReturnUrl);
        return RedirectToRoleHome();
    }

    [HttpGet]
    public IActionResult Register(string? role = null)
    {
        if (_session.IsAuthenticated) return RedirectToRoleHome();
        return View(new RegisterViewModel { Role = role == RoleNames.Organiser ? RoleNames.Organiser : RoleNames.Participant });
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _api.RegisterAsync(new RegisterRequest(
            model.FullName.Trim(), model.Email.Trim(), model.Password, model.Role,
            string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim()));

        if (!result.Success)
        {
            if (result.StatusCode == HttpStatusCode.Conflict)
                ModelState.AddModelError(nameof(model.Email), "An account with this email already exists. Try logging in instead.");
            else
                ModelState.AddApiErrors(result, typeof(RegisterViewModel));
            model.Password = model.ConfirmPassword = string.Empty;
            return View(model);
        }

        // Log the new user straight in.
        var login = await _api.LoginAsync(new LoginRequest(model.Email.Trim(), model.Password));
        if (login.Success && await StartSessionAsync(login.Data!))
        {
            TempData.Success($"Welcome to RaceDay, {FirstName()}! Your {model.Role.ToLower()} account is ready.");
            return RedirectToRoleHome();
        }

        TempData.Success("Your account was created. Please log in.");
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    public IActionResult Logout()
    {
        _session.SignOut();
        TempData.Success("You have been logged out. See you at the start line!");
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }

    private async Task<bool> StartSessionAsync(LoginResponse login)
    {
        var profile = await _api.GetProfileAsync(tokenOverride: login.Token);
        if (!profile.Success || profile.Data is null) return false;
        _session.SignIn(login, profile.Data);
        return true;
    }

    private string FirstName() => (_session.FullName ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "athlete";

    private IActionResult RedirectToRoleHome() =>
        _session.IsOrganiser ? RedirectToAction("Dashboard", "Organiser") : RedirectToAction("Index", "Home");
}
