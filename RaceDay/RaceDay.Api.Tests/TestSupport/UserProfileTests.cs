namespace RaceDay.Api.Tests;

public class UserProfileTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public UserProfileTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetMyProfile_WhenLoggedIn_ReturnsTheProfileWithoutThePassword()
    {
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);

        HttpResponseMessage response = await participant.Client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string json = await response.Content.ReadAsStringAsync();
        Assert.Contains(participant.Email, json);
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetMyProfile_HasNoProfilePictureUntilOneIsUploaded()
    {
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);

        UserResponse? profile = await participant.Client.GetFromJsonAsync<UserResponse>("/api/users/me");

        Assert.Null(profile!.ProfilePictureUrl);
    }

    [Fact]
    public async Task GetMyProfile_WithoutLogin_ReturnsUnauthorized()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMyProfile_ChangesTheDetails()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        var update = new UpdateProfileRequest
        {
            FirstName = "Thabo",
            LastName = "Nkosi",
            PhoneNumber = "0821234567",
            DateOfBirth = new DateOnly(1985, 3, 14)
        };

        HttpResponseMessage response = await organiser.Client.PutAsJsonAsync("/api/users/me", update);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        UserResponse? profile = await organiser.Client.GetFromJsonAsync<UserResponse>("/api/users/me");
        Assert.Equal("Thabo", profile!.FirstName);
        Assert.Equal("Nkosi", profile.LastName);
        Assert.Equal("0821234567", profile.PhoneNumber);
        Assert.Equal(new DateOnly(1985, 3, 14), profile.DateOfBirth);
    }

    [Fact]
    public async Task UpdateMyProfile_EmptyFirstName_ReturnsBadRequest()
    {
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);
        var update = new UpdateProfileRequest { FirstName = "", LastName = "Khumalo" };

        HttpResponseMessage response = await participant.Client.PutAsJsonAsync("/api/users/me", update);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMyProfile_DateOfBirthInTheFuture_ReturnsBadRequest()
    {
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);
        var update = new UpdateProfileRequest
        {
            FirstName = "Sipho",
            LastName = "Khumalo",
            DateOfBirth = DateOnly.FromDateTime(DateTime.Today.AddDays(3))
        };

        HttpResponseMessage response = await participant.Client.PutAsJsonAsync("/api/users/me", update);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMyProfile_WithoutLogin_ReturnsUnauthorized()
    {
        HttpClient client = _factory.CreateClient();
        var update = new UpdateProfileRequest { FirstName = "Sipho", LastName = "Khumalo" };

        HttpResponseMessage response = await client.PutAsJsonAsync("/api/users/me", update);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUserById_AsOrganiser_ReturnsTheUser()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);

        HttpResponseMessage response = await organiser.Client.GetAsync($"/api/users/{participant.UserId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        UserResponse? user = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal(participant.UserId, user!.UserId);
    }

    [Fact]
    public async Task GetUserById_AsParticipant_ReturnsForbidden()
    {
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);
        TestUser other = await TestHelpers.NewParticipantAsync(_factory);

        HttpResponseMessage response = await participant.Client.GetAsync($"/api/users/{other.UserId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetUserById_WithoutLogin_ReturnsUnauthorized()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/users/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUserById_UnknownUser_ReturnsNotFound()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);

        HttpResponseMessage response = await organiser.Client.GetAsync("/api/users/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}