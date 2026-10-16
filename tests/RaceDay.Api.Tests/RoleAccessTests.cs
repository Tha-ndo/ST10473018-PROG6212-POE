namespace RaceDay.Api.Tests;

public class RoleAccessTests : ApiTestBase
{
    public RoleAccessTests(RaceDayFactory factory) : base(factory) { }

    [Fact]
    public async Task Participant_CannotCreateCategory()
    {
        var organiser = await CreateAuthenticatedClientAsync(OrganiserRole);
        var participant = await CreateAuthenticatedClientAsync(ParticipantRole);
        var ev = await CreateEventAsync(organiser);

        var response = await participant.PostAsJsonAsync($"/api/events/{ev.EventId}/categories",
            new CategoryRequest { Name = "5km", DistanceKm = 5m, EntryFee = 0m, MaxParticipants = 10 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotViewEventEnrolments()
    {
        var organiser = await CreateAuthenticatedClientAsync(OrganiserRole);
        var participant = await CreateAuthenticatedClientAsync(ParticipantRole);
        var ev = await CreateEventAsync(organiser);

        var response = await participant.GetAsync($"/api/events/{ev.EventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCaptureResults()
    {
        var organiser = await CreateAuthenticatedClientAsync(OrganiserRole);
        var participant = await CreateAuthenticatedClientAsync(ParticipantRole);
        var ev = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, ev.EventId);
        var enrolment = await EnrolAsync(participant, category.CategoryId);

        var response = await participant.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/results",
            new ResultRequest { FinishTime = TimeSpan.FromMinutes(50), Position = 1, Status = "Finished" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotEnrolInAnEvent()
    {
        var organiser = await CreateAuthenticatedClientAsync(OrganiserRole);
        var ev = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, ev.EventId);

        var response = await organiser.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotViewParticipantOnlyEndpoints()
    {
        var organiser = await CreateAuthenticatedClientAsync(OrganiserRole);

        Assert.Equal(HttpStatusCode.Forbidden, (await organiser.GetAsync("/api/enrolments/my")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await organiser.GetAsync("/api/results/my")).StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotSeeAnotherOrganisersEnrolments()
    {
        var owner = await CreateAuthenticatedClientAsync(OrganiserRole);
        var other = await CreateAuthenticatedClientAsync(OrganiserRole);
        var ev = await CreateEventAsync(owner);

        var response = await other.GetAsync($"/api/events/{ev.EventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task NonPublicEndpoints_RequireAuthentication()
    {
        var anonymous = CreateAnonymousClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/enrolments/my")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/results/my")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsync("/api/categories/1/enrolments", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/events/1/enrolments")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.DeleteAsync("/api/enrolments/1")).StatusCode);
    }
}
