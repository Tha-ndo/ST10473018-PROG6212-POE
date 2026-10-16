namespace RaceDay.Api.Tests;

public class EnrolmentTests : ApiTestBase
{
    public EnrolmentTests(RaceDayFactory factory) : base(factory) { }

    [Fact]
    public async Task Participant_CanEnrol_AndEnrolmentIsRecordedWithBibNumber()
    {
        var organiser = await CreateAuthenticatedClientAsync(OrganiserRole);
        var participant = await CreateAuthenticatedClientAsync(ParticipantRole);
        var ev = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, ev.EventId);

        var response = await participant.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var enrol = await response.Content.ReadFromJsonAsync<EnrolResponse>();
        Assert.True(enrol!.EnrolmentId > 0);
        Assert.False(string.IsNullOrWhiteSpace(enrol.BibNumber));

        var mine = await participant.GetFromJsonAsync<List<EnrolmentDto>>("/api/enrolments/my");
        var recorded = Assert.Single(mine!, e => e.EnrolmentId == enrol.EnrolmentId);
        Assert.Equal(category.CategoryId, recorded.CategoryId);
        Assert.Equal(ev.EventId, recorded.EventId);
        Assert.Equal("Confirmed", recorded.Status);
    }

    [Fact]
    public async Task Organiser_CanSeeEnrolmentsForTheirOwnEvent()
    {
        var organiser = await CreateAuthenticatedClientAsync(OrganiserRole);
        var participant = await CreateAuthenticatedClientAsync(ParticipantRole);
        var ev = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, ev.EventId);
        var enrol = await EnrolAsync(participant, category.CategoryId);

        var response = await organiser.GetAsync($"/api/events/{ev.EventId}/enrolments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<EnrolmentDto>>();
        Assert.Contains(list!, e => e.EnrolmentId == enrol.EnrolmentId);
    }

    [Fact]
    public async Task Enrol_TwiceInTheSameCategory_Returns409()
    {
        var organiser = await CreateAuthenticatedClientAsync(OrganiserRole);
        var participant = await CreateAuthenticatedClientAsync(ParticipantRole);
        var ev = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, ev.EventId);
        await EnrolAsync(participant, category.CategoryId);

        var second = await participant.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_WhenCategoryIsFull_Returns409()
    {
        var organiser = await CreateAuthenticatedClientAsync(OrganiserRole);
        var first = await CreateAuthenticatedClientAsync(ParticipantRole);
        var second = await CreateAuthenticatedClientAsync(ParticipantRole);
        var ev = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, ev.EventId, maxParticipants: 1);
        await EnrolAsync(first, category.CategoryId);

        var response = await second.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_InMissingCategory_Returns404()
    {
        var participant = await CreateAuthenticatedClientAsync(ParticipantRole);

        var response = await participant.PostAsync("/api/categories/999999/enrolments", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Enrolments_GetDifferentBibNumbersWithinAnEvent()
    {
        var organiser = await CreateAuthenticatedClientAsync(OrganiserRole);
        var p1 = await CreateAuthenticatedClientAsync(ParticipantRole);
        var p2 = await CreateAuthenticatedClientAsync(ParticipantRole);
        var ev = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, ev.EventId);

        var e1 = await EnrolAsync(p1, category.CategoryId);
        var e2 = await EnrolAsync(p2, category.CategoryId);

        Assert.NotEqual(e1.BibNumber, e2.BibNumber);
    }

    [Fact]
    public async Task Participant_CanCancelOwnEnrolment_ThenReEnrol()
    {
        var organiser = await CreateAuthenticatedClientAsync(OrganiserRole);
        var participant = await CreateAuthenticatedClientAsync(ParticipantRole);
        var ev = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, ev.EventId);
        var enrol = await EnrolAsync(participant, category.CategoryId);

        var cancel = await participant.DeleteAsync($"/api/enrolments/{enrol.EnrolmentId}");
        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);

        var mine = await participant.GetFromJsonAsync<List<EnrolmentDto>>("/api/enrolments/my");
        Assert.Equal("Cancelled", mine!.Single(e => e.EnrolmentId == enrol.EnrolmentId).Status);

        var again = await participant.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);
        Assert.Equal(HttpStatusCode.Created, again.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCancelSomeoneElsesEnrolment()
    {
        var organiser = await CreateAuthenticatedClientAsync(OrganiserRole);
        var owner = await CreateAuthenticatedClientAsync(ParticipantRole);
        var intruder = await CreateAuthenticatedClientAsync(ParticipantRole);
        var ev = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, ev.EventId);
        var enrol = await EnrolAsync(owner, category.CategoryId);

        var response = await intruder.DeleteAsync($"/api/enrolments/{enrol.EnrolmentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CancelEnrolment_WhenMissing_Returns404()
    {
        var participant = await CreateAuthenticatedClientAsync(ParticipantRole);

        var response = await participant.DeleteAsync("/api/enrolments/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
