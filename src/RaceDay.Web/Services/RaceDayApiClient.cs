using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Services;

/// <summary>
/// Typed HttpClient for the RaceDay Part 2 API. This is the ONLY way the MVC app gets or changes data:
/// there is no database access and no business rules here. The API decides what is valid and the
/// client simply relays its answers (including validation errors) back to the views.
/// </summary>
public class RaceDayApiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly IUserSession _session;
    private readonly ILogger<RaceDayApiClient> _logger;

    public RaceDayApiClient(HttpClient http, IUserSession session, ILogger<RaceDayApiClient> logger)
    {
        _http = http;
        _session = session;
        _logger = logger;
    }

    // ---------- Auth & profile ----------
    public Task<ApiResult<UserDto>> RegisterAsync(RegisterRequest request) => SendAsync<UserDto>(HttpMethod.Post, "api/auth/register", request, anonymous: true);
    public Task<ApiResult<LoginResponse>> LoginAsync(LoginRequest request) => SendAsync<LoginResponse>(HttpMethod.Post, "api/auth/login", request, anonymous: true);
    public Task<ApiResult<UserDto>> GetProfileAsync(string? tokenOverride = null) => SendAsync<UserDto>(HttpMethod.Get, "api/users/me", tokenOverride: tokenOverride);
    public Task<ApiResult<UserDto>> UpdateProfileAsync(UpdateProfileRequest request) => SendAsync<UserDto>(HttpMethod.Put, "api/users/me", request);
    public Task<ApiResult<UserDto>> UploadProfilePictureAsync(IFormFile file) => UploadAsync<UserDto>("api/users/me/photo", file);

    // ---------- Venues ----------
    public Task<ApiResult<List<VenueDto>>> GetVenuesAsync() => SendAsync<List<VenueDto>>(HttpMethod.Get, "api/venues", anonymous: true);
    public Task<ApiResult<VenueDto>> CreateVenueAsync(VenueRequest request) => SendAsync<VenueDto>(HttpMethod.Post, "api/venues", request);

    // ---------- Events ----------
    public Task<ApiResult<List<EventSummaryDto>>> GetUpcomingEventsAsync(string? type, string? province, string? search, DateOnly? from, DateOnly? to)
    {
        var query = new List<string>();
        void Add(string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value)) query.Add($"{key}={Uri.EscapeDataString(value)}");
        }
        Add("type", type);
        Add("province", province);
        Add("search", search);
        Add("from", from?.ToString("yyyy-MM-dd"));
        Add("to", to?.ToString("yyyy-MM-dd"));
        var url = "api/events" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
        return SendAsync<List<EventSummaryDto>>(HttpMethod.Get, url, anonymous: true);
    }

    public Task<ApiResult<EventDetailDto>> GetEventAsync(int id) => SendAsync<EventDetailDto>(HttpMethod.Get, $"api/events/{id}", anonymous: true);
    public Task<ApiResult<List<OrganiserEventDto>>> GetMyEventsAsync() => SendAsync<List<OrganiserEventDto>>(HttpMethod.Get, "api/events/mine");
    public Task<ApiResult<EventDetailDto>> CreateEventAsync(EventRequest request) => SendAsync<EventDetailDto>(HttpMethod.Post, "api/events", request);
    public Task<ApiResult<EventDetailDto>> UpdateEventAsync(int id, EventRequest request) => SendAsync<EventDetailDto>(HttpMethod.Put, $"api/events/{id}", request);
    public Task<ApiResult> DeleteEventAsync(int id) => SendAsync(HttpMethod.Delete, $"api/events/{id}");
    public Task<ApiResult<EventDetailDto>> UploadBannerAsync(int id, IFormFile file) => UploadAsync<EventDetailDto>($"api/events/{id}/banner", file);
    public Task<ApiResult> DeleteBannerAsync(int id) => SendAsync(HttpMethod.Delete, $"api/events/{id}/banner");

    // ---------- Categories ----------
    public Task<ApiResult<CategoryDto>> CreateCategoryAsync(int eventId, CategoryRequest request) => SendAsync<CategoryDto>(HttpMethod.Post, $"api/events/{eventId}/categories", request);
    public Task<ApiResult<CategoryDto>> UpdateCategoryAsync(int id, CategoryRequest request) => SendAsync<CategoryDto>(HttpMethod.Put, $"api/categories/{id}", request);
    public Task<ApiResult> DeleteCategoryAsync(int id) => SendAsync(HttpMethod.Delete, $"api/categories/{id}");

    // ---------- Enrolments ----------
    public Task<ApiResult<EnrolResponse>> EnrolAsync(int categoryId) => SendAsync<EnrolResponse>(HttpMethod.Post, $"api/categories/{categoryId}/enrolments");
    public Task<ApiResult<List<EnrolmentDto>>> GetMyEnrolmentsAsync() => SendAsync<List<EnrolmentDto>>(HttpMethod.Get, "api/enrolments/my");
    public Task<ApiResult<List<EnrolmentDto>>> GetEventEnrolmentsAsync(int eventId) => SendAsync<List<EnrolmentDto>>(HttpMethod.Get, $"api/events/{eventId}/enrolments");
    public Task<ApiResult> CancelEnrolmentAsync(int id) => SendAsync(HttpMethod.Delete, $"api/enrolments/{id}");

    // ---------- Results ----------
    public Task<ApiResult<List<ResultDto>>> GetEventResultsAsync(int eventId) => SendAsync<List<ResultDto>>(HttpMethod.Get, $"api/events/{eventId}/results", anonymous: true);
    public Task<ApiResult<List<ResultDto>>> GetMyResultsAsync() => SendAsync<List<ResultDto>>(HttpMethod.Get, "api/results/my");
    public Task<ApiResult<ResultDto>> CaptureResultAsync(int enrolmentId, ResultRequest request) => SendAsync<ResultDto>(HttpMethod.Post, $"api/enrolments/{enrolmentId}/results", request);
    public Task<ApiResult<ResultDto>> UpdateResultAsync(int resultId, ResultRequest request) => SendAsync<ResultDto>(HttpMethod.Put, $"api/results/{resultId}", request);

    // ---------- Plumbing ----------
    private async Task<ApiResult> SendAsync(HttpMethod method, string url, object? body = null)
    {
        using var request = BuildRequest(method, url, body, anonymous: false, tokenOverride: null);
        using var response = await _http.SendAsync(request);
        if (response.IsSuccessStatusCode) return ApiResult.Ok(response.StatusCode);
        return await ToErrorAsync<object>(response, request);
    }

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, object? body = null, bool anonymous = false, string? tokenOverride = null)
    {
        using var request = BuildRequest(method, url, body, anonymous, tokenOverride);
        using var response = await _http.SendAsync(request);
        return await ReadAsync<T>(response, request);
    }

    private async Task<ApiResult<T>> UploadAsync<T>(string url, IFormFile file)
    {
        using var content = new MultipartFormDataContent();
        var stream = new StreamContent(file.OpenReadStream());
        stream.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType);
        content.Add(stream, "file", Path.GetFileName(file.FileName));

        using var request = BuildRequest(HttpMethod.Post, url, null, anonymous: false, tokenOverride: null);
        request.Content = content;
        using var response = await _http.SendAsync(request);
        return await ReadAsync<T>(response, request);
    }

    private HttpRequestMessage BuildRequest(HttpMethod method, string url, object? body, bool anonymous, string? tokenOverride)
    {
        var request = new HttpRequestMessage(method, url);
        var token = tokenOverride ?? (anonymous ? null : _session.Token);
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
            // Serialised up front so the request carries a Content-Length (no chunked encoding).
            request.Content = new StringContent(JsonSerializer.Serialize(body, body.GetType(), Json), Encoding.UTF8, "application/json");
        return request;
    }

    private async Task<ApiResult<T>> ReadAsync<T>(HttpResponseMessage response, HttpRequestMessage request)
    {
        if (!response.IsSuccessStatusCode) return await ToErrorAsync<T>(response, request);

        if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0)
            return new ApiResult<T> { Success = true, StatusCode = response.StatusCode };

        var data = await response.Content.ReadFromJsonAsync<T>(Json);
        return new ApiResult<T> { Success = true, StatusCode = response.StatusCode, Data = data };
    }

    private async Task<ApiResult<T>> ToErrorAsync<T>(HttpResponseMessage response, HttpRequestMessage request)
    {
        // A token was sent but the API rejected it: the session is stale, so force a fresh login.
        if (response.StatusCode == HttpStatusCode.Unauthorized && request.Headers.Authorization is not null)
            throw new ApiUnauthorizedException();

        string? title = null;
        var fieldErrors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var raw = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(raw))
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("title", out var t)) title = t.GetString();
                    if (root.TryGetProperty("detail", out var d) && d.ValueKind == JsonValueKind.String) title = d.GetString() ?? title;
                    if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var field in errors.EnumerateObject())
                        {
                            var key = field.Name.TrimStart('$', '.');
                            fieldErrors[key] = field.Value.EnumerateArray().Select(e => FriendlyFieldError(e.GetString())).ToArray();
                        }
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Not a ProblemDetails body - fall back to a generic message below.
        }

        var message = response.StatusCode switch
        {
            HttpStatusCode.BadRequest when fieldErrors.Count > 0 => "Please correct the highlighted fields.",
            HttpStatusCode.Unauthorized => title ?? "Please log in to continue.",
            HttpStatusCode.Forbidden => "You don't have permission to do that.",
            HttpStatusCode.NotFound => "We couldn't find what you were looking for.",
            >= HttpStatusCode.InternalServerError => "The RaceDay service hit a problem. Please try again in a moment.",
            _ => title ?? "Something went wrong. Please try again.",
        };

        if ((int)response.StatusCode >= 500)
            _logger.LogError("API {Method} {Url} failed with {Status}", request.Method, request.RequestUri, (int)response.StatusCode);

        return new ApiResult<T> { Success = false, StatusCode = response.StatusCode, Error = message, FieldErrors = fieldErrors };
    }

    /// <summary>Hides raw JSON-parser messages (e.g. "The JSON value could not be converted...") from users.</summary>
    private static string FriendlyFieldError(string? message) =>
        string.IsNullOrEmpty(message) || message.Contains("JSON", StringComparison.OrdinalIgnoreCase) || message.Contains("Path:", StringComparison.Ordinal)
            ? "This value is not in the expected format."
            : message;
}
