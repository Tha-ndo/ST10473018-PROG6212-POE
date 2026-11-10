using System.Text;
using System.Text.Json;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Services;

/// <summary>
/// Session-based authentication state. After a successful login the API's JWT, the user's id, role,
/// name and picture are kept in the server-side ASP.NET Core session; the browser only holds the
/// (HttpOnly) session cookie. Every page reads the user's identity and role from here.
/// </summary>
public interface IUserSession
{
    bool IsAuthenticated { get; }
    string? Token { get; }
    int? UserId { get; }
    string? Role { get; }
    string? FullName { get; }
    string? Email { get; }
    string? ProfilePictureUrl { get; }
    bool IsOrganiser { get; }
    bool IsParticipant { get; }
    string Initials { get; }

    void SignIn(LoginResponse login, UserDto profile);
    void UpdateProfile(UserDto profile);
    void SignOut();
}

public class UserSession : IUserSession
{
    private const string TokenKey = "rd.token";
    private const string UserIdKey = "rd.userId";
    private const string RoleKey = "rd.role";
    private const string NameKey = "rd.name";
    private const string EmailKey = "rd.email";
    private const string PhotoKey = "rd.photo";
    private const string ExpiresKey = "rd.expires";

    private readonly IHttpContextAccessor _accessor;

    public UserSession(IHttpContextAccessor accessor) => _accessor = accessor;

    private ISession? Session => _accessor.HttpContext?.Session;

    public bool IsAuthenticated
    {
        get
        {
            if (string.IsNullOrEmpty(Session?.GetString(TokenKey))) return false;

            // Treat an expired token as logged out, so protected pages redirect to the login page.
            var expires = Session.GetString(ExpiresKey);
            if (long.TryParse(expires, out var unix) && DateTimeOffset.FromUnixTimeSeconds(unix) <= DateTimeOffset.UtcNow)
            {
                SignOut();
                return false;
            }
            return true;
        }
    }

    public string? Token => IsAuthenticated ? Session!.GetString(TokenKey) : null;
    public int? UserId => IsAuthenticated ? Session!.GetInt32(UserIdKey) : null;
    public string? Role => IsAuthenticated ? Session!.GetString(RoleKey) : null;
    public string? FullName => IsAuthenticated ? Session!.GetString(NameKey) : null;
    public string? Email => IsAuthenticated ? Session!.GetString(EmailKey) : null;
    public string? ProfilePictureUrl => IsAuthenticated ? Session!.GetString(PhotoKey) : null;

    public bool IsOrganiser => Role == RoleNames.Organiser;
    public bool IsParticipant => Role == RoleNames.Participant;

    public string Initials
    {
        get
        {
            var parts = (FullName ?? "?").Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return string.Concat(parts.Take(2).Select(p => char.ToUpperInvariant(p[0])));
        }
    }

    public void SignIn(LoginResponse login, UserDto profile)
    {
        var session = Session ?? throw new InvalidOperationException("Session is not available.");
        session.Clear();
        session.SetString(TokenKey, login.Token);
        session.SetInt32(UserIdKey, login.UserId);
        session.SetString(RoleKey, login.Role);
        var exp = ReadExpiry(login.Token);
        if (exp is not null) session.SetString(ExpiresKey, exp.Value.ToString());
        UpdateProfile(profile);
    }

    public void UpdateProfile(UserDto profile)
    {
        var session = Session ?? throw new InvalidOperationException("Session is not available.");
        session.SetString(NameKey, profile.FullName);
        session.SetString(EmailKey, profile.Email);
        if (string.IsNullOrEmpty(profile.ProfilePictureUrl)) session.Remove(PhotoKey);
        else session.SetString(PhotoKey, profile.ProfilePictureUrl);
    }

    public void SignOut() => Session?.Clear();

    /// <summary>Reads the "exp" claim from the JWT payload (no signature check needed: the API validates the token).</summary>
    private static long? ReadExpiry(string jwt)
    {
        try
        {
            var payload = jwt.Split('.')[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
            return doc.RootElement.TryGetProperty("exp", out var exp) ? exp.GetInt64() : null;
        }
        catch
        {
            return null;
        }
    }
}
