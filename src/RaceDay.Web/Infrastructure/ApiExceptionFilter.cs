using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using RaceDay.Web.Services;

namespace RaceDay.Web.Infrastructure;

/// <summary>
/// Turns API-level failures into friendly pages instead of raw exceptions:
/// an expired token sends the user back to login; an unreachable API shows a "service unavailable" page.
/// </summary>
public class ApiExceptionFilter : IExceptionFilter
{
    private readonly IUserSession _session;
    private readonly ITempDataDictionaryFactory _tempData;
    private readonly IModelMetadataProvider _metadata;
    private readonly ILogger<ApiExceptionFilter> _logger;

    public ApiExceptionFilter(IUserSession session, ITempDataDictionaryFactory tempData, IModelMetadataProvider metadata, ILogger<ApiExceptionFilter> logger)
    {
        _session = session;
        _tempData = tempData;
        _metadata = metadata;
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case ApiUnauthorizedException ex:
                _session.SignOut();
                _tempData.GetTempData(context.HttpContext).Warning(ex.Message);
                var req = context.HttpContext.Request;
                var returnUrl = req.Method == HttpMethods.Get ? req.Path + req.QueryString : null;
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
                context.ExceptionHandled = true;
                break;

            case HttpRequestException or TaskCanceledException:
                _logger.LogError(context.Exception, "The RaceDay API could not be reached.");
                context.Result = new ViewResult
                {
                    ViewName = "ServiceUnavailable",
                    StatusCode = StatusCodes.Status503ServiceUnavailable,
                    ViewData = new ViewDataDictionary(_metadata, context.ModelState),
                };
                context.ExceptionHandled = true;
                break;
        }
    }
}
