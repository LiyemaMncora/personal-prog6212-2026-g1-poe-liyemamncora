namespace RaceDay.Api.Tests;

public class CategoryTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public CategoryTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCategoriesForEvent_AsParticipant_ReturnsThemSortedByDistance()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        EventResponse ev = await TestHelpers.CreateEventAsync(organiser);
        await TestHelpers.CreateCategoryAsync(organiser, ev.EventId, "21.1km Half Marathon", 21.1m);
        await TestHelpers.CreateCategoryAsync(organiser, ev.EventId, "5km Fun Run", 5m);
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);

        HttpResponseMessage response = await participant.Client.GetAsync($"/api/events/{ev.EventId}/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<CategoryResponse>? categories = await response.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        Assert.Equal(2, categories!.Count);
        Assert.Equal("5km Fun Run", categories[0].Name);
        Assert.Equal("21.1km Half Marathon", categories[1].Name);
    }

    [Fact]
    public async Task GetCategoriesForEvent_AsOrganiser_ReturnsTheCategories()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        var (ev, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);

        List<CategoryResponse>? categories = await organiser.Client.GetFromJsonAsync<List<CategoryResponse>>($"/api/events/{ev.EventId}/categories");

        Assert.Contains(categories!, c => c.CategoryId == category.CategoryId);
    }

    [Fact]
    public async Task GetCategoriesForEvent_WithoutLogin_ReturnsUnauthorized()
    {
        HttpClient anonymous = _factory.CreateClient();

        HttpResponseMessage response = await anonymous.GetAsync("/api/events/1/categories");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCategoriesForEvent_UnknownEvent_ReturnsNotFound()
    {
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);

        HttpResponseMessage response = await participant.Client.GetAsync("/api/events/999999/categories");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetCategory_ById_ReturnsTheDistanceFeeAndStartTime()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);

        HttpResponseMessage response = await participant.Client.GetAsync($"/api/categories/{category.CategoryId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        CategoryResponse? body = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        Assert.Equal(10m, body!.DistanceKm);
        Assert.Equal(120m, body.EntryFee);
        Assert.Equal(new TimeSpan(6, 0, 0), body.StartTime);
    }

    [Fact]
    public async Task GetCategory_UnknownId_ReturnsNotFound()
    {
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);

        HttpResponseMessage response = await participant.Client.GetAsync("/api/categories/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetCategory_WithoutLogin_ReturnsUnauthorized()
    {
        HttpClient anonymous = _factory.CreateClient();

        HttpResponseMessage response = await anonymous.GetAsync("/api/categories/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_AsOrganiser_ReturnsCreated()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        EventResponse ev = await TestHelpers.CreateEventAsync(organiser);

        HttpResponseMessage response = await organiser.Client.PostAsJsonAsync(
            $"/api/events/{ev.EventId}/categories", TestHelpers.ValidCategoryRequest("10km Run", 10, 800));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        CategoryResponse? category = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        Assert.Equal(ev.EventId, category!.EventId);
        Assert.Equal("10km Run", category.Name);
        Assert.Equal(800, category.MaxParticipants);
        Assert.Equal(0, category.EnrolmentCount);
    }

    [Fact]
    public async Task CreateCategory_AsParticipant_ReturnsForbidden()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);
        EventResponse ev = await TestHelpers.CreateEventAsync(organiser);

        HttpResponseMessage response = await participant.Client.PostAsJsonAsync(
            $"/api/events/{ev.EventId}/categories", TestHelpers.ValidCategoryRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_WithoutLogin_ReturnsUnauthorized()
    {
        HttpClient anonymous = _factory.CreateClient();

        HttpResponseMessage response = await anonymous.PostAsJsonAsync("/api/events/1/categories", TestHelpers.ValidCategoryRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_InAnotherOrganisersEvent_ReturnsForbidden()
    {
        TestUser owner = await TestHelpers.NewOrganiserAsync(_factory);
        TestUser otherOrganiser = await TestHelpers.NewOrganiserAsync(_factory);
        EventResponse ev = await TestHelpers.CreateEventAsync(owner);

        HttpResponseMessage response = await otherOrganiser.Client.PostAsJsonAsync(
            $"/api/events/{ev.EventId}/categories", TestHelpers.ValidCategoryRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_UnknownEvent_ReturnsNotFound()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);

        HttpResponseMessage response = await organiser.Client.PostAsJsonAsync(
            "/api/events/999999/categories", TestHelpers.ValidCategoryRequest());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_DistanceOfZero_ReturnsBadRequest()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        EventResponse ev = await TestHelpers.CreateEventAsync(organiser);

        HttpResponseMessage response = await organiser.Client.PostAsJsonAsync(
            $"/api/events/{ev.EventId}/categories", TestHelpers.ValidCategoryRequest("Zero", 0));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_NegativeFee_ReturnsBadRequest()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        EventResponse ev = await TestHelpers.CreateEventAsync(organiser);
        CategoryRequest request = TestHelpers.ValidCategoryRequest();
        request.EntryFee = -5;

        HttpResponseMessage response = await organiser.Client.PostAsJsonAsync($"/api/events/{ev.EventId}/categories", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_StartTimeOver24Hours_ReturnsBadRequest()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        EventResponse ev = await TestHelpers.CreateEventAsync(organiser);
        CategoryRequest request = TestHelpers.ValidCategoryRequest();
        request.StartTime = TimeSpan.FromHours(25);

        HttpResponseMessage response = await organiser.Client.PostAsJsonAsync($"/api/events/{ev.EventId}/categories", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_SameNameTwiceInOneEvent_ReturnsConflict()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        EventResponse ev = await TestHelpers.CreateEventAsync(organiser);
        await TestHelpers.CreateCategoryAsync(organiser, ev.EventId, "10km Run");

        HttpResponseMessage response = await organiser.Client.PostAsJsonAsync(
            $"/api/events/{ev.EventId}/categories", TestHelpers.ValidCategoryRequest("10KM RUN"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_SameNameInADifferentEvent_ReturnsCreated()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        EventResponse first = await TestHelpers.CreateEventAsync(organiser);
        EventResponse second = await TestHelpers.CreateEventAsync(organiser);
        await TestHelpers.CreateCategoryAsync(organiser, first.EventId, "10km Run");

        HttpResponseMessage response = await organiser.Client.PostAsJsonAsync(
            $"/api/events/{second.EventId}/categories", TestHelpers.ValidCategoryRequest("10km Run"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCategory_OwnCategory_ReturnsTheUpdatedCategory()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);
        CategoryRequest update = TestHelpers.ValidCategoryRequest("Half Marathon", 21.1m, 500);
        update.EntryFee = 180;

        HttpResponseMessage response = await organiser.Client.PutAsJsonAsync($"/api/categories/{category.CategoryId}", update);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        CategoryResponse? saved = await organiser.Client.GetFromJsonAsync<CategoryResponse>($"/api/categories/{category.CategoryId}");
        Assert.Equal("Half Marathon", saved!.Name);
        Assert.Equal(21.1m, saved.DistanceKm);
        Assert.Equal(180m, saved.EntryFee);
        Assert.Equal(500, saved.MaxParticipants);
    }

    [Fact]
    public async Task UpdateCategory_AnotherOrganisersCategory_ReturnsForbidden()
    {
        TestUser owner = await TestHelpers.NewOrganiserAsync(_factory);
        TestUser otherOrganiser = await TestHelpers.NewOrganiserAsync(_factory);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(owner);

        HttpResponseMessage response = await otherOrganiser.Client.PutAsJsonAsync(
            $"/api/categories/{category.CategoryId}", TestHelpers.ValidCategoryRequest("Changed"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCategory_AsParticipant_ReturnsForbidden()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);

        HttpResponseMessage response = await participant.Client.PutAsJsonAsync(
            $"/api/categories/{category.CategoryId}", TestHelpers.ValidCategoryRequest("Changed"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCategory_UnknownId_ReturnsNotFound()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);

        HttpResponseMessage response = await organiser.Client.PutAsJsonAsync("/api/categories/999999", TestHelpers.ValidCategoryRequest());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCategory_NameOfAnotherCategoryInTheEvent_ReturnsConflict()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        EventResponse ev = await TestHelpers.CreateEventAsync(organiser);
        await TestHelpers.CreateCategoryAsync(organiser, ev.EventId, "10km Run", 10);
        CategoryResponse second = await TestHelpers.CreateCategoryAsync(organiser, ev.EventId, "5km Fun Run", 5);

        HttpResponseMessage response = await organiser.Client.PutAsJsonAsync(
            $"/api/categories/{second.CategoryId}", TestHelpers.ValidCategoryRequest("10km Run", 5));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCategory_KeepingItsOwnName_ReturnsOk()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);
        CategoryRequest update = TestHelpers.ValidCategoryRequest(category.Name, 10);
        update.EntryFee = 150;

        HttpResponseMessage response = await organiser.Client.PutAsJsonAsync($"/api/categories/{category.CategoryId}", update);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCategory_LimitLowerThanTheCurrentEntries_ReturnsConflict()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser, maxParticipants: 10);
        TestUser first = await TestHelpers.NewParticipantAsync(_factory);
        TestUser second = await TestHelpers.NewParticipantAsync(_factory);
        await TestHelpers.EnrolAsync(first, category.CategoryId);
        await TestHelpers.EnrolAsync(second, category.CategoryId);

        HttpResponseMessage response = await organiser.Client.PutAsJsonAsync(
            $"/api/categories/{category.CategoryId}", TestHelpers.ValidCategoryRequest(category.Name, 10, 1));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_OwnCategory_ReturnsNoContentAndItIsGone()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);

        HttpResponseMessage delete = await organiser.Client.DeleteAsync($"/api/categories/{category.CategoryId}");
        HttpResponseMessage afterDelete = await organiser.Client.GetAsync($"/api/categories/{category.CategoryId}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_WithParticipantsEntered_ReturnsConflict()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);
        await TestHelpers.EnrolAsync(participant, category.CategoryId);

        HttpResponseMessage response = await organiser.Client.DeleteAsync($"/api/categories/{category.CategoryId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_AnotherOrganisersCategory_ReturnsForbidden()
    {
        TestUser owner = await TestHelpers.NewOrganiserAsync(_factory);
        TestUser otherOrganiser = await TestHelpers.NewOrganiserAsync(_factory);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(owner);

        HttpResponseMessage response = await otherOrganiser.Client.DeleteAsync($"/api/categories/{category.CategoryId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_AsParticipant_ReturnsForbidden()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);

        HttpResponseMessage response = await participant.Client.DeleteAsync($"/api/categories/{category.CategoryId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_UnknownId_ReturnsNotFound()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);

        HttpResponseMessage response = await organiser.Client.DeleteAsync("/api/categories/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}