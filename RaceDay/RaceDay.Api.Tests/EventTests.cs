namespace RaceDay.Api.Tests;

public class EventTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public EventTests(RaceDayFactory factory)
    {
        _factory = factory;
    }


    [Fact]
    public async Task GetEvents_WithoutLogin_ReturnsUnauthorized()
    {
        HttpClient anonymous = _factory.CreateClient();

        HttpResponseMessage response = await anonymous.GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetEvents_AsOrganiser_ReturnsTheEvents()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        EventResponse created = await TestHelpers.CreateEventAsync(organiser);

        List<EventResponse>? events = await organiser.Client.GetFromJsonAsync<List<EventResponse>>("/api/events");

        Assert.Contains(events!, e => e.EventId == created.EventId);
    }

    [Fact]
    public async Task GetEvents_AsParticipant_ReturnsTheEvents()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        EventResponse created = await TestHelpers.CreateEventAsync(organiser);
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);

        List<EventResponse>? events = await participant.Client.GetFromJsonAsync<List<EventResponse>>("/api/events");

        Assert.Contains(events!, e => e.EventId == created.EventId);
    }

    [Fact]
    public async Task GetEvent_ById_ReturnsTheEventWithItsLocation()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        EventResponse created = await TestHelpers.CreateEventAsync(organiser);
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);

        HttpResponseMessage response = await participant.Client.GetAsync($"/api/events/{created.EventId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        EventResponse? ev = await response.Content.ReadFromJsonAsync<EventResponse>();
        Assert.Equal(created.Name, ev!.Name);
        Assert.Equal(-29.8673m, ev.Latitude);
        Assert.Equal(31.0454m, ev.Longitude);
        Assert.Equal(organiser.UserId, ev.OrganiserId);
    }

    [Fact]
    public async Task GetEvent_UnknownId_ReturnsNotFound()
    {
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);

        HttpResponseMessage response = await participant.Client.GetAsync("/api/events/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetEvent_WithoutLogin_ReturnsUnauthorized()
    {
        HttpClient anonymous = _factory.CreateClient();

        HttpResponseMessage response = await anonymous.GetAsync("/api/events/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyEvents_AsOrganiser_ReturnsOnlyTheirOwnEvents()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        TestUser otherOrganiser = await TestHelpers.NewOrganiserAsync(_factory);
        EventResponse mine = await TestHelpers.CreateEventAsync(organiser);
        EventResponse theirs = await TestHelpers.CreateEventAsync(otherOrganiser);

        List<EventResponse>? events = await organiser.Client.GetFromJsonAsync<List<EventResponse>>("/api/events/my-events");

        Assert.Contains(events!, e => e.EventId == mine.EventId);
        Assert.DoesNotContain(events!, e => e.EventId == theirs.EventId);
        Assert.All(events!, e => Assert.Equal(organiser.UserId, e.OrganiserId));
    }

    [Fact]
    public async Task GetMyEvents_AsParticipant_ReturnsForbidden()
    {
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);

        HttpResponseMessage response = await participant.Client.GetAsync("/api/events/my-events");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetMyEvents_WithoutLogin_ReturnsUnauthorized()
    {
        HttpClient anonymous = _factory.CreateClient();

        HttpResponseMessage response = await anonymous.GetAsync("/api/events/my-events");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetEvent_ReturnsTheDistanceAndNoBannerYet()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        EventResponse created = await TestHelpers.CreateEventAsync(organiser);

        EventResponse? ev = await organiser.Client.GetFromJsonAsync<EventResponse>($"/api/events/{created.EventId}");

        Assert.Equal(10m, ev!.DistanceKm);
        Assert.Null(ev.BannerImageUrl);
        Assert.Equal(0, ev.EnrolmentCount);
    }


    [Fact]
    public async Task GetEvents_FilterByEventType_ReturnsOnlyThatType()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        EventResponse running = await TestHelpers.CreateEventAsync(organiser);
        HttpResponseMessage cyclingResponse = await organiser.Client.PostAsJsonAsync("/api/events", TestHelpers.ValidEventRequest("Cycling"));
        EventResponse cycling = (await cyclingResponse.Content.ReadFromJsonAsync<EventResponse>())!;

        List<EventResponse>? events = await organiser.Client.GetFromJsonAsync<List<EventResponse>>("/api/events?eventType=cycle");

        Assert.Contains(events!, e => e.EventId == cycling.EventId);
        Assert.DoesNotContain(events!, e => e.EventId == running.EventId);
        Assert.All(events!, e => Assert.Equal("Cycling", e.EventType));
    }

    [Fact]
    public async Task GetEvents_FilterByInvalidEventType_ReturnsBadRequest()
    {
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);

        HttpResponseMessage response = await participant.Client.GetAsync("/api/events?eventType=Swimming");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetEvents_FilterByProvince_ReturnsOnlyThatProvince()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        EventResponse inKzn = await TestHelpers.CreateEventAsync(organiser);
        EventRequest capeRequest = TestHelpers.ValidEventRequest();
        capeRequest.Province = "Western Cape";
        HttpResponseMessage capeResponse = await organiser.Client.PostAsJsonAsync("/api/events", capeRequest);
        EventResponse inCape = (await capeResponse.Content.ReadFromJsonAsync<EventResponse>())!;

        List<EventResponse>? events = await organiser.Client.GetFromJsonAsync<List<EventResponse>>("/api/events?province=western%20cape");

        Assert.Contains(events!, e => e.EventId == inCape.EventId);
        Assert.DoesNotContain(events!, e => e.EventId == inKzn.EventId);
    }

    [Fact]
    public async Task GetEvents_Search_FindsTheEventByPartOfItsName()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        EventRequest request = TestHelpers.ValidEventRequest();
        request.Name = "Zululand Sunrise Fun Run";
        HttpResponseMessage createResponse = await organiser.Client.PostAsJsonAsync("/api/events", request);
        EventResponse created = (await createResponse.Content.ReadFromJsonAsync<EventResponse>())!;
        EventResponse other = await TestHelpers.CreateEventAsync(organiser);

        List<EventResponse>? events = await organiser.Client.GetFromJsonAsync<List<EventResponse>>("/api/events?search=sunrise");

        Assert.Contains(events!, e => e.EventId == created.EventId);
        Assert.DoesNotContain(events!, e => e.EventId == other.EventId);
    }

    [Fact]
    public async Task GetEvents_UpcomingOnly_LeavesOutEventsThatAlreadyTookPlace()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        EventResponse upcoming = await TestHelpers.CreateEventAsync(organiser);
        int pastCategoryId = await TestHelpers.SeedPastCategoryAsync(_factory, organiser.UserId);
        CategoryResponse? pastCategory = await organiser.Client.GetFromJsonAsync<CategoryResponse>($"/api/categories/{pastCategoryId}");
        int pastEventId = pastCategory!.EventId;

        List<EventResponse>? all = await organiser.Client.GetFromJsonAsync<List<EventResponse>>("/api/events");
        List<EventResponse>? upcomingOnly = await organiser.Client.GetFromJsonAsync<List<EventResponse>>("/api/events?upcomingOnly=true");

        Assert.Contains(all!, e => e.EventId == pastEventId);
        Assert.DoesNotContain(upcomingOnly!, e => e.EventId == pastEventId);
        Assert.Contains(upcomingOnly!, e => e.EventId == upcoming.EventId);
    }


    [Fact]
    public async Task GetMyEvents_ShowsHowManyParticipantsAreEnrolled()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        TestUser first = await TestHelpers.NewParticipantAsync(_factory);
        TestUser second = await TestHelpers.NewParticipantAsync(_factory);
        TestUser third = await TestHelpers.NewParticipantAsync(_factory);
        var (ev, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);
        await TestHelpers.EnrolAsync(first, category.CategoryId);
        await TestHelpers.EnrolAsync(second, category.CategoryId);
        EnrolmentResponse thirdEnrolment = await TestHelpers.EnrolAsync(third, category.CategoryId);

        await third.Client.DeleteAsync($"/api/enrolments/{thirdEnrolment.EnrolmentId}");

        List<EventResponse>? events = await organiser.Client.GetFromJsonAsync<List<EventResponse>>("/api/events/my-events");

        EventResponse mine = Assert.Single(events!, e => e.EventId == ev.EventId);
        Assert.Equal(2, mine.EnrolmentCount);
    }

    [Fact]
    public async Task CreateEvent_AsOrganiser_ReturnsCreatedAndUsesTheLoggedInOrganiser()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);

        HttpResponseMessage response = await organiser.Client.PostAsJsonAsync("/api/events", TestHelpers.ValidEventRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        EventResponse? ev = await response.Content.ReadFromJsonAsync<EventResponse>();
        Assert.True(ev!.EventId > 0);
        Assert.Equal(organiser.UserId, ev.OrganiserId);
        Assert.Equal("Running", ev.EventType);
    }

    [Fact]
    public async Task CreateEvent_AsParticipant_ReturnsForbidden()
    {
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);

        HttpResponseMessage response = await participant.Client.PostAsJsonAsync("/api/events", TestHelpers.ValidEventRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithoutLogin_ReturnsUnauthorized()
    {
        HttpClient anonymous = _factory.CreateClient();

        HttpResponseMessage response = await anonymous.PostAsJsonAsync("/api/events", TestHelpers.ValidEventRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_DateInThePast_ReturnsBadRequest()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        EventRequest request = TestHelpers.ValidEventRequest();
        request.EventDate = DateTime.Now.AddDays(-1);

        HttpResponseMessage response = await organiser.Client.PostAsJsonAsync("/api/events", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_InvalidEventType_ReturnsBadRequest()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);

        HttpResponseMessage response = await organiser.Client.PostAsJsonAsync("/api/events", TestHelpers.ValidEventRequest("Swimming"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_MissingName_ReturnsBadRequest()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        EventRequest request = TestHelpers.ValidEventRequest();
        request.Name = "";

        HttpResponseMessage response = await organiser.Client.PostAsJsonAsync("/api/events", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_SavesTheDistance()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        EventRequest request = TestHelpers.ValidEventRequest();
        request.DistanceKm = 21.1m;

        HttpResponseMessage response = await organiser.Client.PostAsJsonAsync("/api/events", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        EventResponse? ev = await response.Content.ReadFromJsonAsync<EventResponse>();
        Assert.Equal(21.1m, ev!.DistanceKm);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task CreateEvent_DistanceZeroOrNegative_ReturnsBadRequest(int distance)
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        EventRequest request = TestHelpers.ValidEventRequest();
        request.DistanceKm = distance;

        HttpResponseMessage response = await organiser.Client.PostAsJsonAsync("/api/events", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_LatitudeOutOfRange_ReturnsBadRequest()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        EventRequest request = TestHelpers.ValidEventRequest();
        request.Latitude = 91m;

        HttpResponseMessage response = await organiser.Client.PostAsJsonAsync("/api/events", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("run", "Running")]
    [InlineData("WALK", "Walking")]
    [InlineData("Cycle", "Cycling")]
    [InlineData("cycling", "Cycling")]
    public async Task CreateEvent_ShortOrLowerCaseEventType_IsSavedAsTheFullName(string typed, string expected)
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);

        HttpResponseMessage response = await organiser.Client.PostAsJsonAsync("/api/events", TestHelpers.ValidEventRequest(typed));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        EventResponse? ev = await response.Content.ReadFromJsonAsync<EventResponse>();
        Assert.Equal(expected, ev!.EventType);
    }


    [Fact]
    public async Task UpdateEvent_OwnEvent_ReturnsTheUpdatedEvent()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        EventResponse created = await TestHelpers.CreateEventAsync(organiser);
        EventRequest update = TestHelpers.ValidEventRequest("Walking");
        update.Name = "Renamed Race";
        update.Location = "Pietermaritzburg";
        update.DistanceKm = 5m;

        HttpResponseMessage response = await organiser.Client.PutAsJsonAsync($"/api/events/{created.EventId}", update);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        EventResponse? ev = await organiser.Client.GetFromJsonAsync<EventResponse>($"/api/events/{created.EventId}");
        Assert.Equal("Renamed Race", ev!.Name);
        Assert.Equal("Pietermaritzburg", ev.Location);
        Assert.Equal("Walking", ev.EventType);
        Assert.Equal(5m, ev.DistanceKm);
    }

    [Fact]
    public async Task UpdateEvent_AnotherOrganisersEvent_ReturnsForbidden()
    {
        TestUser owner = await TestHelpers.NewOrganiserAsync(_factory);
        TestUser otherOrganiser = await TestHelpers.NewOrganiserAsync(_factory);
        EventResponse created = await TestHelpers.CreateEventAsync(owner);
        EventRequest update = TestHelpers.ValidEventRequest();
        update.Name = "Stolen Race";

        HttpResponseMessage response = await otherOrganiser.Client.PutAsJsonAsync($"/api/events/{created.EventId}", update);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        EventResponse? ev = await owner.Client.GetFromJsonAsync<EventResponse>($"/api/events/{created.EventId}");
        Assert.Equal(created.Name, ev!.Name);
    }

    [Fact]
    public async Task UpdateEvent_AsParticipant_ReturnsForbidden()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);
        EventResponse created = await TestHelpers.CreateEventAsync(organiser);

        HttpResponseMessage response = await participant.Client.PutAsJsonAsync($"/api/events/{created.EventId}", TestHelpers.ValidEventRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_UnknownId_ReturnsNotFound()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);

        HttpResponseMessage response = await organiser.Client.PutAsJsonAsync("/api/events/999999", TestHelpers.ValidEventRequest());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_MovingTheDateToThePast_ReturnsBadRequest()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        EventResponse created = await TestHelpers.CreateEventAsync(organiser);
        EventRequest update = TestHelpers.ValidEventRequest();
        update.EventDate = DateTime.Now.AddDays(-2);

        HttpResponseMessage response = await organiser.Client.PutAsJsonAsync($"/api/events/{created.EventId}", update);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_OwnEvent_ReturnsNoContentAndTheEventIsGone()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        EventResponse created = await TestHelpers.CreateEventAsync(organiser);

        HttpResponseMessage delete = await organiser.Client.DeleteAsync($"/api/events/{created.EventId}");
        HttpResponseMessage afterDelete = await organiser.Client.GetAsync($"/api/events/{created.EventId}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_AlsoDeletesItsCategoriesAndEnrolments()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);
        var (ev, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);
        EnrolmentResponse enrolment = await TestHelpers.EnrolAsync(participant, category.CategoryId);

        HttpResponseMessage delete = await organiser.Client.DeleteAsync($"/api/events/{ev.EventId}");
        HttpResponseMessage categoryAfter = await participant.Client.GetAsync($"/api/categories/{category.CategoryId}");
        HttpResponseMessage enrolmentAfter = await participant.Client.GetAsync($"/api/enrolments/{enrolment.EnrolmentId}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, categoryAfter.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, enrolmentAfter.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_AnotherOrganisersEvent_ReturnsForbiddenAndTheEventStays()
    {
        TestUser owner = await TestHelpers.NewOrganiserAsync(_factory);
        TestUser otherOrganiser = await TestHelpers.NewOrganiserAsync(_factory);
        EventResponse created = await TestHelpers.CreateEventAsync(owner);

        HttpResponseMessage delete = await otherOrganiser.Client.DeleteAsync($"/api/events/{created.EventId}");
        HttpResponseMessage afterDelete = await owner.Client.GetAsync($"/api/events/{created.EventId}");

        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
        Assert.Equal(HttpStatusCode.OK, afterDelete.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_AsParticipant_ReturnsForbidden()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);
        EventResponse created = await TestHelpers.CreateEventAsync(organiser);

        HttpResponseMessage response = await participant.Client.DeleteAsync($"/api/events/{created.EventId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_WithoutLogin_ReturnsUnauthorized()
    {
        HttpClient anonymous = _factory.CreateClient();

        HttpResponseMessage response = await anonymous.DeleteAsync("/api/events/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_UnknownId_ReturnsNotFound()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);

        HttpResponseMessage response = await organiser.Client.DeleteAsync("/api/events/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}