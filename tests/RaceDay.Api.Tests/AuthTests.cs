
namespace RaceDay.Api.Tests;

public class AuthTests : ApiTestBase
{
    public AuthTests(RaceDayFactory factory) : base(factory) { }

    private static RegisterRequest ValidRegistration(string email, string role = "Participant") => new()
    {
        FullName = "Sipho Nkosi",
        Email = email,
        Password = Password,
        Role = role,
        PhoneNumber = "0731112222",
    };

    [Theory]
    [InlineData("Participant")]
    [InlineData("Organiser")]
    public async Task Register_WithValidDetails_Returns201AndDoesNotExposePassword(string role)
    {
        var client = CreateAnonymousClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", ValidRegistration(UniqueEmail("reg"), role));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", raw, StringComparison.OrdinalIgnoreCase);
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.Equal(role, user!.Role);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        var client = CreateAnonymousClient();
        var email = UniqueEmail("dup");
        (await client.PostAsJsonAsync("/api/auth/register", ValidRegistration(email))).EnsureSuccessStatusCode();

        var second = await client.PostAsJsonAsync("/api/auth/register", ValidRegistration(email));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidRole_Returns400()
    {
        var client = CreateAnonymousClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", ValidRegistration(UniqueEmail("role"), "Admin"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithShortPassword_Returns400()
    {
        var client = CreateAnonymousClient();
        var request = ValidRegistration(UniqueEmail("short"));
        request.Password = "abc";

        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_StoresHashedPassword_NotThePlainText()
    {
        var client = CreateAnonymousClient();
        var email = UniqueEmail("hash");
        (await client.PostAsJsonAsync("/api/auth/register", ValidRegistration(email))).EnsureSuccessStatusCode();

        var stored = WithDb(db => db.Users.Single(u => u.Email == email).PasswordHash);

        Assert.NotEqual(Password, stored);
        Assert.DoesNotContain(Password, stored);
        Assert.False(string.IsNullOrWhiteSpace(stored));
    }

    [Theory]
    [InlineData("Participant")]
    [InlineData("Organiser")]
    public async Task Login_WithValidCredentials_ReturnsTokenAndRole(string role)
    {
        var client = CreateAnonymousClient();
        var email = UniqueEmail("login");
        (await client.PostAsJsonAsync("/api/auth/register", ValidRegistration(email, role))).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
        Assert.Equal(role, body.Role);
        Assert.True(body.UserId > 0);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = CreateAnonymousClient();
        var email = UniqueEmail("wrongpw");
        (await client.PostAsJsonAsync("/api/auth/register", ValidRegistration(email))).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = "Wrong-password-1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        var client = CreateAnonymousClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = UniqueEmail("ghost"), Password = Password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_SeededOrganiser_CanLogInWithDemoPassword()
    {
        var client = CreateAnonymousClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "thandiwe@comrades.org.za", Password = Data.DbSeeder.DemoPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var client = CreateAnonymousClient();

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithGarbageToken_Returns401()
    {
        var client = CreateAnonymousClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "not-a-real-token");

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_WhenLoggedIn_ReturnsOwnProfile()
    {
        var email = UniqueEmail("me");
        var client = await CreateAuthenticatedClientAsync(ParticipantRole, email);

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.Equal(email, me!.Email);
        Assert.Equal(ParticipantRole, me.Role);
    }

    [Theory]
    [InlineData("Participant")]
    [InlineData("Organiser")]
    public async Task UpdateMe_BothRolesCanUpdateTheirOwnProfile(string role)
    {
        var client = await CreateAuthenticatedClientAsync(role);

        var response = await client.PutAsJsonAsync("/api/users/me", new UpdateProfileRequest { FullName = "New Name", PhoneNumber = "0839998888" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.Equal("New Name", updated!.FullName);
        Assert.Equal("0839998888", updated.PhoneNumber);
        Assert.Equal(role, updated.Role);
    }

    [Fact]
    public async Task UpdateMe_WithMissingName_Returns400()
    {
        var client = await CreateAuthenticatedClientAsync(ParticipantRole);

        var response = await client.PutAsJsonAsync("/api/users/me", new UpdateProfileRequest { FullName = "", PhoneNumber = "0839998888" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetUserById_AsOrganiser_Returns200_AsParticipant_Returns403()
    {
        var organiser = await CreateAuthenticatedClientAsync(OrganiserRole);
        var participant = await CreateAuthenticatedClientAsync(ParticipantRole);
        var participantId = (await (await participant.GetAsync("/api/users/me")).Content.ReadFromJsonAsync<UserDto>())!.UserId;

        var asOrganiser = await organiser.GetAsync($"/api/users/{participantId}");
        var asParticipant = await participant.GetAsync($"/api/users/{participantId}");

        Assert.Equal(HttpStatusCode.OK, asOrganiser.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, asParticipant.StatusCode);
    }

    [Fact]
    public async Task GetUserById_WhenMissing_Returns404()
    {
        var organiser = await CreateAuthenticatedClientAsync(OrganiserRole);

        var response = await organiser.GetAsync("/api/users/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
