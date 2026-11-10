using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Services;

namespace RaceDay.Web.Infrastructure;

/// <summary>
/// Protects a controller or action using the session. Anonymous users are redirected to the login page
/// (and brought back afterwards); logged-in users with the wrong role are sent to the Access Denied page.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireLoginAttribute : ActionFilterAttribute
{
    private readonly string[] _roles;

    public RequireLoginAttribute(params string[] roles) => _roles = roles;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var http = context.HttpContext;
        var session = http.RequestServices.GetRequiredService<IUserSession>();

        // Protected pages must never be served from the browser cache after logout.
        http.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        http.Response.Headers.Pragma = "no-cache";

        if (!session.IsAuthenticated)
        {
            if (context.Controller is Controller c) c.TempData.Warning("Please log in to continue.");
            var returnUrl = http.Request.Method == HttpMethods.Get ? http.Request.Path + http.Request.QueryString : null;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }

        if (_roles.Length > 0 && !_roles.Contains(session.Role))
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
    }
}
