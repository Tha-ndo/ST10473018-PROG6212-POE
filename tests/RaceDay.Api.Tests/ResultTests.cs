namespace RaceDay.Api.Tests;

public class ResultTests : ApiTestBase
{
    public ResultTests(RaceDayFactory factory) : base(factory) { }

    private static ResultRequest Finished(int position = 1, int minutes = 50) => new()
    {
        FinishTime = TimeSpan.FromMinutes(minutes), Position = position, Status = "Finished",
    };

    [Fact]
    public async Task Organiser_CanCaptureResult_ParticipantSeesItInTheirResults()
    {
        var organiser = await CreateAuthenticatedClientAsync(OrganiserRole);
        var participant = await CreateAuthenticatedClientAsync(ParticipantRole);
        var ev = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, ev.EventId);
        var enrol = await EnrolAsync(participant, category.CategoryId);

        var response = await organiser.PostAsJsonAsync($"/api/enrolments/{enrol.EnrolmentId}/results", Finished(position: 3, minutes: 47));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ResultDto>();
        Assert.Equal(3, created!.Position);
        Assert.Equal(TimeSpan.FromMinutes(47), created.FinishTime);

        var mine = await participant.GetFromJsonAsync<List<ResultDto>>("/api/results/my");
        Assert.Contains(mine!, r => r.ResultId == created.ResultId);
    }

    [Fact]
    public async Task EventResults_ArePublic_AndOrderedByPosition()
    {
        var organiser = await CreateAuthenticatedClientAsync(OrganiserRole);
        var p1 = await CreateAuthenticatedClientAsync(ParticipantRole);
        var p2 = await CreateAuthenticatedClientAsync(ParticipantRole);
        var ev = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, ev.EventId);
        var e1 = await EnrolAsync(p1, category.CategoryId);
        var e2 = await EnrolAsync(p2, category.CategoryId);
        (await organiser.PostAsJsonAsync($"/api/enrolments/{e1.EnrolmentId}/results", Finished(position: 2, minutes: 55))).EnsureSuccessStatusCode();
        (await organiser.PostAsJsonAsync($"/api/enrolments/{e2.EnrolmentId}/results", Finished(position: 1, minutes: 49))).EnsureSuccessStatusCode();

        var response = await CreateAnonymousClient().GetAsync($"/api/events/{ev.EventId}/results");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<ResultDto>>();
        Assert.Equal(new int?[] { 1, 2 }, list!.Select(r => r.Position).ToArray());
    }

    [Fact]
    public async Task EventResults_ForMissingEvent_Returns404()
    {
        var response = await CreateAnonymousClient().GetAsync("/api/events/999999/results");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_ByADifferentOrganiser_Returns403()
    {
        var owner = await CreateAuthenticatedClientAsync(OrganiserRole);
        var other = await CreateAuthenticatedClientAsync(OrganiserRole);
        var participant = await CreateAuthenticatedClientAsync(ParticipantRole);
        var ev = await CreateEventAsync(owner);
        var category = await CreateCategoryAsync(owner, ev.EventId);
        var enrol = await EnrolAsync(participant, category.CategoryId);

        var response = await other.PostAsJsonAsync($"/api/enrolments/{enrol.EnrolmentId}/results", Finished());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_FinishedWithoutTimeOrPosition_Returns400()
    {
        var organiser = await CreateAuthenticatedClientAsync(OrganiserRole);
        var participant = await CreateAuthenticatedClientAsync(ParticipantRole);
        var ev = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, ev.EventId);
        var enrol = await EnrolAsync(participant, category.CategoryId);

        var response = await organiser.PostAsJsonAsync($"/api/enrolments/{enrol.EnrolmentId}/results", new ResultRequest { Status = "Finished" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_DnfWithoutTime_IsAccepted()
    {
        var organiser = await CreateAuthenticatedClientAsync(OrganiserRole);
        var participant = await CreateAuthenticatedClientAsync(ParticipantRole);
        var ev = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, ev.EventId);
        var enrol = await EnrolAsync(participant, category.CategoryId);

        var response = await organiser.PostAsJsonAsync($"/api/enrolments/{enrol.EnrolmentId}/results", new ResultRequest { Status = "DNF" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_Twice_Returns409()
    {
        var organiser = await CreateAuthenticatedClientAsync(OrganiserRole);
        var participant = await CreateAuthenticatedClientAsync(ParticipantRole);
        var ev = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, ev.EventId);
        var enrol = await EnrolAsync(participant, category.CategoryId);
        (await organiser.PostAsJsonAsync($"/api/enrolments/{enrol.EnrolmentId}/results", Finished())).EnsureSuccessStatusCode();

        var second = await organiser.PostAsJsonAsync($"/api/enrolments/{enrol.EnrolmentId}/results", Finished(position: 2));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_ForMissingEnrolment_Returns404()
    {
        var organiser = await CreateAuthenticatedClientAsync(OrganiserRole);

        var response = await organiser.PostAsJsonAsync("/api/enrolments/999999/results", Finished());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateResult_ByOwner_Returns200_ByOtherOrganiser_Returns403()
    {
        var owner = await CreateAuthenticatedClientAsync(OrganiserRole);
        var other = await CreateAuthenticatedClientAsync(OrganiserRole);
        var participant = await CreateAuthenticatedClientAsync(ParticipantRole);
        var ev = await CreateEventAsync(owner);
        var category = await CreateCategoryAsync(owner, ev.EventId);
        var enrol = await EnrolAsync(participant, category.CategoryId);
        var created = await (await owner.PostAsJsonAsync($"/api/enrolments/{enrol.EnrolmentId}/results", Finished(position: 5)))
            .Content.ReadFromJsonAsync<ResultDto>();

        var byOther = await other.PutAsJsonAsync($"/api/results/{created!.ResultId}", Finished(position: 1));
        var byOwner = await owner.PutAsJsonAsync($"/api/results/{created.ResultId}", Finished(position: 4, minutes: 45));

        Assert.Equal(HttpStatusCode.Forbidden, byOther.StatusCode);
        Assert.Equal(HttpStatusCode.OK, byOwner.StatusCode);
        var updated = await byOwner.Content.ReadFromJsonAsync<ResultDto>();
        Assert.Equal(4, updated!.Position);
    }

    [Fact]
    public async Task UpdateResult_WhenMissing_Returns404()
    {
        var organiser = await CreateAuthenticatedClientAsync(OrganiserRole);

        var response = await organiser.PutAsJsonAsync("/api/results/999999", Finished());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Participant_OnlySeesTheirOwnResults()
    {
        var organiser = await CreateAuthenticatedClientAsync(OrganiserRole);
        var runner = await CreateAuthenticatedClientAsync(ParticipantRole);
        var bystander = await CreateAuthenticatedClientAsync(ParticipantRole);
        var ev = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, ev.EventId);
        var enrol = await EnrolAsync(runner, category.CategoryId);
        (await organiser.PostAsJsonAsync($"/api/enrolments/{enrol.EnrolmentId}/results", Finished())).EnsureSuccessStatusCode();

        var bystanderResults = await bystander.GetFromJsonAsync<List<ResultDto>>("/api/results/my");

        Assert.Empty(bystanderResults!);
    }
}
