using System.Net.Http.Headers;

namespace RaceDay.Api.Tests;

/// <summary>Tests for the endpoints added to support the Part 3 MVC front end.</summary>
public class Part3ApiTests : ApiTestBase
{
    public Part3ApiTests(RaceDayFactory factory) : base(factory) { }

    private static MultipartFormDataContent ImageForm(string fileName = "banner.png", int bytes = 128)
    {
        var file = new ByteArrayContent(new byte[bytes]);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        return new MultipartFormDataContent { { file, "file", fileName } };
    }

    [Fact]
    public async Task MyEvents_ReturnsOnlyTheOrganisersEvents_WithEnrolmentTotals()
    {
        var organiser = await CreateAuthenticatedClientAsync(OrganiserRole);
        var other = await CreateAuthenticatedClientAsync(OrganiserRole);
        var participant = await CreateAuthenticatedClientAsync(ParticipantRole);
        var mine = await CreateEventAsync(organiser);
        await CreateEventAsync(other);
        var category = await CreateCategoryAsync(organiser, mine.EventId, maxParticipants: 50);
        await EnrolAsync(participant, category.CategoryId);

        var list = await organiser.GetFromJsonAsync<List<OrganiserEventDto>>("/api/events/mine");

        var row = Assert.Single(list!);
        Assert.Equal(mine.EventId, row.EventId);
        Assert.Equal(1, row.ConfirmedEnrolments);
        Assert.Equal(50, row.TotalCapacity);
    }

    [Fact]
    public async Task MyEvents_AsParticipant_Returns403()
    {
        var participant = await CreateAuthenticatedClientAsync(ParticipantRole);

        var response = await participant.GetAsync("/api/events/mine");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpcomingEvents_CanBeFilteredBySearchAndType()
    {
        var organiser = await CreateAuthenticatedClientAsync(OrganiserRole);
        var unique = $"Filter{Guid.NewGuid():N}";
        var ev = await CreateEventAsync(organiser, title: unique);

        var running = await CreateAnonymousClient().GetFromJsonAsync<List<EventSummaryDto>>($"/api/events?search={unique}&type=Running");
        var cycling = await CreateAnonymousClient().GetFromJsonAsync<List<EventSummaryDto>>($"/api/events?search={unique}&type=Cycling");

        Assert.Equal(ev.EventId, Assert.Single(running!).EventId);
        Assert.Empty(cycling!);
    }

    [Fact]
    public async Task Organiser_CanUploadBanner_AndItAppearsOnEventDetail()
    {
        var organiser = await CreateAuthenticatedClientAsync(OrganiserRole);
        var ev = await CreateEventAsync(organiser);

        var response = await organiser.PostAsync($"/api/events/{ev.EventId}/banner", ImageForm());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await CreateAnonymousClient().GetFromJsonAsync<EventDetailDto>($"/api/events/{ev.EventId}");
        Assert.StartsWith(FakeFileStorage.BaseUrl + "event-banners/", detail!.BannerImageUrl);
    }

    [Fact]
    public async Task BannerUpload_RejectsNonImageFiles()
    {
        var organiser = await CreateAuthenticatedClientAsync(OrganiserRole);
        var ev = await CreateEventAsync(organiser);

        var response = await organiser.PostAsync($"/api/events/{ev.EventId}/banner", ImageForm("notes.txt"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task BannerUpload_ByAnotherOrganiser_Returns403()
    {
        var owner = await CreateAuthenticatedClientAsync(OrganiserRole);
        var intruder = await CreateAuthenticatedClientAsync(OrganiserRole);
        var ev = await CreateEventAsync(owner);

        var response = await intruder.PostAsync($"/api/events/{ev.EventId}/banner", ImageForm());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task User_CanUploadProfilePicture()
    {
        var participant = await CreateAuthenticatedClientAsync(ParticipantRole);

        var response = await participant.PostAsync("/api/users/me/photo", ImageForm("me.png"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await participant.GetFromJsonAsync<UserDto>("/api/users/me");
        Assert.StartsWith(FakeFileStorage.BaseUrl + "profile-pictures/", me!.ProfilePictureUrl);
    }

    [Fact]
    public async Task MyResults_IncludeTotalFinishersInTheCategory()
    {
        var organiser = await CreateAuthenticatedClientAsync(OrganiserRole);
        var p1 = await CreateAuthenticatedClientAsync(ParticipantRole);
        var p2 = await CreateAuthenticatedClientAsync(ParticipantRole);
        var ev = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, ev.EventId);
        var e1 = await EnrolAsync(p1, category.CategoryId);
        var e2 = await EnrolAsync(p2, category.CategoryId);
        var finished = new ResultRequest { FinishTime = TimeSpan.FromMinutes(50), Position = 1, Status = "Finished" };
        (await organiser.PostAsJsonAsync($"/api/enrolments/{e1.EnrolmentId}/results", finished)).EnsureSuccessStatusCode();
        (await organiser.PostAsJsonAsync($"/api/enrolments/{e2.EnrolmentId}/results",
            new ResultRequest { FinishTime = TimeSpan.FromMinutes(55), Position = 2, Status = "Finished" })).EnsureSuccessStatusCode();

        var mine = await p1.GetFromJsonAsync<List<ResultDto>>("/api/results/my");

        var result = Assert.Single(mine!);
        Assert.Equal(1, result.Position);
        Assert.Equal(2, result.TotalFinishers);
    }
}
