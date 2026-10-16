using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Models;

namespace RaceDay.Api.Tests;

public abstract class ApiTestBase : IClassFixture<RaceDayFactory>
{
    protected const string Password = "Passw0rd!123";

    protected RaceDayFactory Factory { get; }

    protected ApiTestBase(RaceDayFactory factory) => Factory = factory;

    protected HttpClient CreateAnonymousClient() => Factory.CreateClient();

    protected static string UniqueEmail(string prefix) => $"{prefix}-{Guid.NewGuid():N}@test.co.za";

    protected static DateOnly FutureDate(int days = 60) => DateOnly.FromDateTime(DateTime.Today.AddDays(days));

    protected async Task<HttpClient> CreateAuthenticatedClientAsync(string role, string? email = null)
    {
        var client = Factory.CreateClient();
        email ??= UniqueEmail(role.ToLowerInvariant());

        var register = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FullName = $"Test {role}",
            Email = email,
            Password = Password,
            Role = role,
            PhoneNumber = "0821234567",
        });
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = Password });
        login.EnsureSuccessStatusCode();
        var body = (await login.Content.ReadFromJsonAsync<LoginResponse>())!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.Token);
        return client;
    }

    protected async Task<EventDetailDto> CreateEventAsync(HttpClient organiser, string? title = null)
    {
        var venues = await organiser.GetFromJsonAsync<List<VenueDto>>("/api/venues");
        var response = await organiser.PostAsJsonAsync("/api/events", new EventRequest
        {
            Title = title ?? $"Test Run {Guid.NewGuid():N}",
            Description = "A test event",
            EventDate = FutureDate(),
            EventType = "Running",
            VenueId = venues![0].VenueId,
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EventDetailDto>())!;
    }

    protected async Task<CategoryDto> CreateCategoryAsync(HttpClient organiser, int eventId, int maxParticipants = 100, string name = "10km")
    {
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new CategoryRequest
        {
            Name = name,
            DistanceKm = 10m,
            EntryFee = 150m,
            MaxParticipants = maxParticipants,
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CategoryDto>())!;
    }

    protected async Task<EnrolResponse> EnrolAsync(HttpClient participant, int categoryId)
    {
        var response = await participant.PostAsync($"/api/categories/{categoryId}/enrolments", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EnrolResponse>())!;
    }

    protected T WithDb<T>(Func<Data.RaceDayDbContext, T> action)
    {
        using var scope = Factory.Services.CreateScope();
        return action(scope.ServiceProvider.GetRequiredService<Data.RaceDayDbContext>());
    }

    protected static string OrganiserRole => Roles.Organiser;
    protected static string ParticipantRole => Roles.Participant;
}
