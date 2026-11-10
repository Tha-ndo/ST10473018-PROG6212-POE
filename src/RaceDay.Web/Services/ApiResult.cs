using System.Net;

namespace RaceDay.Web.Services;

/// <summary>Outcome of an API call: success flag, a user-friendly message and any per-field validation errors.</summary>
public class ApiResult
{
    public bool Success { get; init; }
    public HttpStatusCode StatusCode { get; init; }
    public string? Error { get; init; }
    public IReadOnlyDictionary<string, string[]> FieldErrors { get; init; } = new Dictionary<string, string[]>();

    public static ApiResult Ok(HttpStatusCode status) => new() { Success = true, StatusCode = status };
}

public class ApiResult<T> : ApiResult
{
    public T? Data { get; init; }
}

/// <summary>Thrown when the API rejects the session token (expired or invalid), so the user must log in again.</summary>
public class ApiUnauthorizedException : Exception
{
    public ApiUnauthorizedException() : base("Your session has expired. Please log in again.") { }
}
