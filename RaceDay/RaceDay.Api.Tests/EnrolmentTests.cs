namespace RaceDay.Api.Tests;

public class EnrolmentTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public EnrolmentTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Enrol_AsParticipant_ReturnsCreatedWithARaceNumber()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);
        var (ev, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);

        HttpResponseMessage response = await participant.Client.PostAsJsonAsync(
            "/api/enrolments", new CreateEnrolmentRequest { CategoryId = category.CategoryId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        EnrolmentResponse? enrolment = await response.Content.ReadFromJsonAsync<EnrolmentResponse>();
        Assert.Equal("Confirmed", enrolment!.Status);

        Assert.Equal($"T{ev.EventId}001", enrolment.RaceNumber);
    }

    [Fact]
    public async Task Enrol_IsRecordedForTheParticipantTheEventAndTheCategory()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);
        var (ev, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);

        EnrolmentResponse enrolment = await TestHelpers.EnrolAsync(participant, category.CategoryId);

        EnrolmentResponse? saved = await participant.Client.GetFromJsonAsync<EnrolmentResponse>($"/api/enrolments/{enrolment.EnrolmentId}");
        Assert.Equal(participant.UserId, saved!.ParticipantId);
        Assert.Equal(ev.EventId, saved.EventId);
        Assert.Equal(category.CategoryId, saved.CategoryId);
        Assert.Equal(ev.Name, saved.EventName);
        Assert.Equal(category.Name, saved.CategoryName);

        List<EnrolmentResponse>? mine = await participant.Client.GetFromJsonAsync<List<EnrolmentResponse>>("/api/enrolments/me");
        Assert.Contains(mine!, e => e.EnrolmentId == enrolment.EnrolmentId);
    }

    [Fact]
    public async Task Enrol_SavesTheEventIdOnTheEnrolmentRow()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);
        var (ev, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);

        EnrolmentResponse enrolment = await TestHelpers.EnrolAsync(participant, category.CategoryId);

        int storedEventId = await TestHelpers.GetStoredEventIdAsync(_factory, enrolment.EnrolmentId);
        Assert.Equal(ev.EventId, storedEventId);
        Assert.Equal(ev.EventId, enrolment.EventId);
    }

    [Fact]
    public async Task Enrol_SecondParticipantInTheSameEvent_GetsTheNextRaceNumber()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        TestUser first = await TestHelpers.NewParticipantAsync(_factory);
        TestUser second = await TestHelpers.NewParticipantAsync(_factory);
        var (ev, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);

        EnrolmentResponse firstEnrolment = await TestHelpers.EnrolAsync(first, category.CategoryId);
        EnrolmentResponse secondEnrolment = await TestHelpers.EnrolAsync(second, category.CategoryId);

        Assert.Equal($"T{ev.EventId}001", firstEnrolment.RaceNumber);
        Assert.Equal($"T{ev.EventId}002", secondEnrolment.RaceNumber);
    }

    [Fact]
    public async Task Enrol_AsOrganiser_ReturnsForbidden()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);

        HttpResponseMessage response = await organiser.Client.PostAsJsonAsync(
            "/api/enrolments", new CreateEnrolmentRequest { CategoryId = category.CategoryId });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_WithoutLogin_ReturnsUnauthorized()
    {
        HttpClient anonymous = _factory.CreateClient();

        HttpResponseMessage response = await anonymous.PostAsJsonAsync(
            "/api/enrolments", new CreateEnrolmentRequest { CategoryId = 1 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_UnknownCategory_ReturnsNotFound()
    {
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);

        HttpResponseMessage response = await participant.Client.PostAsJsonAsync(
            "/api/enrolments", new CreateEnrolmentRequest { CategoryId = 999999 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_MissingCategoryId_ReturnsBadRequest()
    {
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);

        HttpResponseMessage response = await participant.Client.PostAsJsonAsync("/api/enrolments", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_SameCategoryTwice_ReturnsConflict()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);
        await TestHelpers.EnrolAsync(participant, category.CategoryId);

        HttpResponseMessage response = await participant.Client.PostAsJsonAsync(
            "/api/enrolments", new CreateEnrolmentRequest { CategoryId = category.CategoryId });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_FullCategory_ReturnsConflict()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        TestUser first = await TestHelpers.NewParticipantAsync(_factory);
        TestUser second = await TestHelpers.NewParticipantAsync(_factory);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser, maxParticipants: 1);
        await TestHelpers.EnrolAsync(first, category.CategoryId);

        HttpResponseMessage response = await second.Client.PostAsJsonAsync(
            "/api/enrolments", new CreateEnrolmentRequest { CategoryId = category.CategoryId });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_EventThatAlreadyTookPlace_ReturnsConflict()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);
        int categoryId = await TestHelpers.SeedPastCategoryAsync(_factory, organiser.UserId);

        HttpResponseMessage response = await participant.Client.PostAsJsonAsync(
            "/api/enrolments", new CreateEnrolmentRequest { CategoryId = categoryId });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_OwnEnrolment_ReturnsNoContentAndTheStatusIsCancelled()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);
        EnrolmentResponse enrolment = await TestHelpers.EnrolAsync(participant, category.CategoryId);

        HttpResponseMessage response = await participant.Client.DeleteAsync($"/api/enrolments/{enrolment.EnrolmentId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        EnrolmentResponse? saved = await participant.Client.GetFromJsonAsync<EnrolmentResponse>($"/api/enrolments/{enrolment.EnrolmentId}");
        Assert.Equal("Cancelled", saved!.Status);
    }

    [Fact]
    public async Task Cancel_GivesThePlaceBackToTheCategory()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        TestUser first = await TestHelpers.NewParticipantAsync(_factory);
        TestUser second = await TestHelpers.NewParticipantAsync(_factory);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser, maxParticipants: 1);
        EnrolmentResponse firstEnrolment = await TestHelpers.EnrolAsync(first, category.CategoryId);
        await first.Client.DeleteAsync($"/api/enrolments/{firstEnrolment.EnrolmentId}");

        HttpResponseMessage response = await second.Client.PostAsJsonAsync(
            "/api/enrolments", new CreateEnrolmentRequest { CategoryId = category.CategoryId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_AfterCancelling_ConfirmsTheSameEnrolmentAgain()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);
        EnrolmentResponse original = await TestHelpers.EnrolAsync(participant, category.CategoryId);
        await participant.Client.DeleteAsync($"/api/enrolments/{original.EnrolmentId}");

        EnrolmentResponse again = await TestHelpers.EnrolAsync(participant, category.CategoryId);

        Assert.Equal(original.EnrolmentId, again.EnrolmentId);
        Assert.Equal(original.RaceNumber, again.RaceNumber);
        Assert.Equal("Confirmed", again.Status);
    }

    [Fact]
    public async Task Cancel_AnotherParticipantsEnrolment_ReturnsForbidden()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        TestUser owner = await TestHelpers.NewParticipantAsync(_factory);
        TestUser other = await TestHelpers.NewParticipantAsync(_factory);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);
        EnrolmentResponse enrolment = await TestHelpers.EnrolAsync(owner, category.CategoryId);

        HttpResponseMessage response = await other.Client.DeleteAsync($"/api/enrolments/{enrolment.EnrolmentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        EnrolmentResponse? saved = await owner.Client.GetFromJsonAsync<EnrolmentResponse>($"/api/enrolments/{enrolment.EnrolmentId}");
        Assert.Equal("Confirmed", saved!.Status);
    }

    [Fact]
    public async Task Cancel_AsOrganiser_ReturnsForbidden()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);
        EnrolmentResponse enrolment = await TestHelpers.EnrolAsync(participant, category.CategoryId);

        HttpResponseMessage response = await organiser.Client.DeleteAsync($"/api/enrolments/{enrolment.EnrolmentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_UnknownEnrolment_ReturnsNotFound()
    {
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);

        HttpResponseMessage response = await participant.Client.DeleteAsync("/api/enrolments/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_EventThatAlreadyTookPlace_ReturnsConflict()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);
        var (_, _, enrolmentId) = await TestHelpers.SeedPastEnrolmentAsync(_factory, organiser.UserId, participant.UserId);

        HttpResponseMessage response = await participant.Client.DeleteAsync($"/api/enrolments/{enrolmentId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetMyEnrolments_AsParticipant_ReturnsOnlyTheirOwn()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        TestUser first = await TestHelpers.NewParticipantAsync(_factory);
        TestUser second = await TestHelpers.NewParticipantAsync(_factory);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);
        EnrolmentResponse firstEnrolment = await TestHelpers.EnrolAsync(first, category.CategoryId);
        EnrolmentResponse secondEnrolment = await TestHelpers.EnrolAsync(second, category.CategoryId);

        List<EnrolmentResponse>? mine = await first.Client.GetFromJsonAsync<List<EnrolmentResponse>>("/api/enrolments/me");

        Assert.Contains(mine!, e => e.EnrolmentId == firstEnrolment.EnrolmentId);
        Assert.DoesNotContain(mine!, e => e.EnrolmentId == secondEnrolment.EnrolmentId);
        Assert.All(mine!, e => Assert.Equal(first.UserId, e.ParticipantId));
    }

    [Fact]
    public async Task GetMyEnrolments_AsOrganiser_ReturnsForbidden()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);

        HttpResponseMessage response = await organiser.Client.GetAsync("/api/enrolments/me");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetMyEnrolments_WithoutLogin_ReturnsUnauthorized()
    {
        HttpClient anonymous = _factory.CreateClient();

        HttpResponseMessage response = await anonymous.GetAsync("/api/enrolments/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }


    [Fact]
    public async Task GetEnrolment_AsTheOrganiserOfTheEvent_ReturnsOk()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);
        EnrolmentResponse enrolment = await TestHelpers.EnrolAsync(participant, category.CategoryId);

        HttpResponseMessage response = await organiser.Client.GetAsync($"/api/enrolments/{enrolment.EnrolmentId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetEnrolment_AsAnotherParticipant_ReturnsForbidden()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        TestUser owner = await TestHelpers.NewParticipantAsync(_factory);
        TestUser other = await TestHelpers.NewParticipantAsync(_factory);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);
        EnrolmentResponse enrolment = await TestHelpers.EnrolAsync(owner, category.CategoryId);

        HttpResponseMessage response = await other.Client.GetAsync($"/api/enrolments/{enrolment.EnrolmentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetEnrolment_AsAnOrganiserOfAnotherEvent_ReturnsForbidden()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        TestUser otherOrganiser = await TestHelpers.NewOrganiserAsync(_factory);
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);
        EnrolmentResponse enrolment = await TestHelpers.EnrolAsync(participant, category.CategoryId);

        HttpResponseMessage response = await otherOrganiser.Client.GetAsync($"/api/enrolments/{enrolment.EnrolmentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetEnrolment_UnknownId_ReturnsNotFound()
    {
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);

        HttpResponseMessage response = await participant.Client.GetAsync("/api/enrolments/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }


    [Fact]
    public async Task GetEnrolmentsForEvent_AsOrganiser_ReturnsTheParticipantsWithTheirNames()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        TestUser first = await TestHelpers.NewParticipantAsync(_factory);
        TestUser second = await TestHelpers.NewParticipantAsync(_factory);
        var (ev, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);
        await TestHelpers.EnrolAsync(first, category.CategoryId);
        await TestHelpers.EnrolAsync(second, category.CategoryId);

        HttpResponseMessage response = await organiser.Client.GetAsync($"/api/events/{ev.EventId}/enrolments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<EnrolmentResponse>? list = await response.Content.ReadFromJsonAsync<List<EnrolmentResponse>>();
        Assert.Equal(2, list!.Count);
        Assert.Contains(list, e => e.ParticipantId == first.UserId);
        Assert.Contains(list, e => e.ParticipantId == second.UserId);
        Assert.All(list, e => Assert.Equal("Test Participant", e.ParticipantName));
    }

    [Fact]
    public async Task GetEnrolmentsForEvent_ForAnotherOrganisersEvent_ReturnsForbidden()
    {
        TestUser owner = await TestHelpers.NewOrganiserAsync(_factory);
        TestUser otherOrganiser = await TestHelpers.NewOrganiserAsync(_factory);
        EventResponse ev = await TestHelpers.CreateEventAsync(owner);

        HttpResponseMessage response = await otherOrganiser.Client.GetAsync($"/api/events/{ev.EventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetEnrolmentsForEvent_AsParticipant_ReturnsForbidden()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);
        EventResponse ev = await TestHelpers.CreateEventAsync(organiser);

        HttpResponseMessage response = await participant.Client.GetAsync($"/api/events/{ev.EventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetEnrolmentsForEvent_UnknownEvent_ReturnsNotFound()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);

        HttpResponseMessage response = await organiser.Client.GetAsync("/api/events/999999/enrolments");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}