namespace RaceDay.Api.Tests;

public class EventTests : ApiTestBase
{
    public EventTests(RaceDayFactory factory) : base(factory) { }

    [Fact]
    public async Task GetEvents_Anonymous_ReturnsSeededUpcomingEvents()
    {
        var client = CreateAnonymousClient();

        var response = await client.GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var events = await response.Content.ReadFromJsonAsync<List<EventSummaryDto>>();
        Assert.NotEmpty(events!);
    }

    [Fact]
    public async Task CreateEvent_AsOrganiser_Returns201_AndEventIsVisibleToEveryone()
    {
        var organiser = await CreateAuthenticatedClientAsync(OrganiserRole);

        var created = await CreateEventAsync(organiser, "Organiser Created 10km");

        var fetched = await CreateAnonymousClient().GetAsync($"/api/events/{created.EventId}");
        Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
        var detail = await fetched.Content.ReadFromJsonAsync<EventDetailDto>();
        Assert.Equal("Organiser Created 10km", detail!.Title);
        Assert.NotNull(detail.Venue);
    }

    [Fact]
    public async Task CreateEvent_AsParticipant_Returns403()
    {
        var participant = await CreateAuthenticatedClientAsync(ParticipantRole);

        var response = await participant.PostAsJsonAsync("/api/events", new EventRequest
        {
            Title = "Nope", EventDate = FutureDate(), EventType = "Running", VenueId = 1,
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_Unauthenticated_Returns401()
    {
        var response = await CreateAnonymousClient().PostAsJsonAsync("/api/events", new EventRequest
        {
            Title = "Nope", EventDate = FutureDate(), EventType = "Running", VenueId = 1,
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithInvalidEventType_Returns400()
    {
        var organiser = await CreateAuthenticatedClientAsync(OrganiserRole);

        var response = await organiser.PostAsJsonAsync("/api/events", new EventRequest
        {
            Title = "Bad type", EventDate = FutureDate(), EventType = "Swimming", VenueId = 1,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithPastDate_Returns400()
    {
        var organiser = await CreateAuthenticatedClientAsync(OrganiserRole);

        var response = await organiser.PostAsJsonAsync("/api/events", new EventRequest
        {
            Title = "Yesterday", EventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)), EventType = "Running", VenueId = 1,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithUnknownVenue_Returns400()
    {
        var organiser = await CreateAuthenticatedClientAsync(OrganiserRole);

        var response = await organiser.PostAsJsonAsync("/api/events", new EventRequest
        {
            Title = "No venue", EventDate = FutureDate(), EventType = "Running", VenueId = 999999,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetEvent_WhenMissing_Returns404()
    {
        var response = await CreateAnonymousClient().GetAsync("/api/events/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_ByOwner_Returns200AndPersistsChanges()
    {
        var organiser = await CreateAuthenticatedClientAsync(OrganiserRole);
        var ev = await CreateEventAsync(organiser);

        var response = await organiser.PutAsJsonAsync($"/api/events/{ev.EventId}", new EventRequest
        {
            Title = "Renamed", Description = "Updated", EventDate = FutureDate(90), EventType = "Walking", VenueId = ev.Venue.VenueId,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<EventDetailDto>();
        Assert.Equal("Renamed", updated!.Title);
        Assert.Equal("Walking", updated.EventType);
    }

    [Fact]
    public async Task UpdateEvent_ByDifferentOrganiser_Returns403()
    {
        var owner = await CreateAuthenticatedClientAsync(OrganiserRole);
        var other = await CreateAuthenticatedClientAsync(OrganiserRole);
        var ev = await CreateEventAsync(owner);

        var response = await other.PutAsJsonAsync($"/api/events/{ev.EventId}", new EventRequest
        {
            Title = "Hijacked", EventDate = FutureDate(), EventType = "Running", VenueId = ev.Venue.VenueId,
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_WhenMissing_Returns404()
    {
        var organiser = await CreateAuthenticatedClientAsync(OrganiserRole);

        var response = await organiser.PutAsJsonAsync("/api/events/999999", new EventRequest
        {
            Title = "Ghost", EventDate = FutureDate(), EventType = "Running", VenueId = 1,
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_ByOwner_Returns204ThenEventIsGone()
    {
        var organiser = await CreateAuthenticatedClientAsync(OrganiserRole);
        var ev = await CreateEventAsync(organiser);

        var delete = await organiser.DeleteAsync($"/api/events/{ev.EventId}");
        var afterwards = await organiser.GetAsync($"/api/events/{ev.EventId}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, afterwards.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_ByDifferentOrganiser_Returns403()
    {
        var owner = await CreateAuthenticatedClientAsync(OrganiserRole);
        var other = await CreateAuthenticatedClientAsync(OrganiserRole);
        var ev = await CreateEventAsync(owner);

        var response = await other.DeleteAsync($"/api/events/{ev.EventId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_WithEnrolments_Returns409()
    {
        var organiser = await CreateAuthenticatedClientAsync(OrganiserRole);
        var participant = await CreateAuthenticatedClientAsync(ParticipantRole);
        var ev = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, ev.EventId);
        await EnrolAsync(participant, category.CategoryId);

        var response = await organiser.DeleteAsync($"/api/events/{ev.EventId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Categories_OrganiserCanAddViewUpdateAndDelete_OwnerOnly()
    {
        var owner = await CreateAuthenticatedClientAsync(OrganiserRole);
        var other = await CreateAuthenticatedClientAsync(OrganiserRole);
        var ev = await CreateEventAsync(owner);

        var category = await CreateCategoryAsync(owner, ev.EventId, name: "Half Marathon");

        var listed = await CreateAnonymousClient().GetFromJsonAsync<List<CategoryDto>>($"/api/events/{ev.EventId}/categories");
        Assert.Contains(listed!, c => c.CategoryId == category.CategoryId);

        var body = new CategoryRequest { Name = "Half (renamed)", DistanceKm = 21.1m, EntryFee = 300m, MaxParticipants = 500 };
        Assert.Equal(HttpStatusCode.Forbidden, (await other.PutAsJsonAsync($"/api/categories/{category.CategoryId}", body)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync($"/api/categories/{category.CategoryId}", body)).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await other.DeleteAsync($"/api/categories/{category.CategoryId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/categories/{category.CategoryId}")).StatusCode);
    }

    [Fact]
    public async Task GetCategories_ForMissingEvent_Returns404()
    {
        var response = await CreateAnonymousClient().GetAsync("/api/events/999999/categories");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_WithInvalidDistance_Returns400()
    {
        var organiser = await CreateAuthenticatedClientAsync(OrganiserRole);
        var ev = await CreateEventAsync(organiser);

        var response = await organiser.PostAsJsonAsync($"/api/events/{ev.EventId}/categories",
            new CategoryRequest { Name = "Zero", DistanceKm = 0m, EntryFee = 0m, MaxParticipants = 10 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
