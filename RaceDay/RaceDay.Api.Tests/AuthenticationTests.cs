using RaceDay.Api.Data;
using RaceDay.Api.Helpers;

namespace RaceDay.Api.Tests;

public class AuthenticationTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public AuthenticationTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    private static RegisterRequest NewRegisterRequest(string roleName = "Participant", string? email = null)
    {
        return new RegisterRequest
        {
            FirstName = "Sipho",
            LastName = "Khumalo",
            Email = email ?? $"user.{Guid.NewGuid():N}@test.co.za",
            Password = TestHelpers.Password,
            PhoneNumber = "0712223333",
            DateOfBirth = new DateOnly(1996, 11, 2),
            RoleName = roleName
        };
    }


    [Fact]
    public async Task Register_AsParticipant_ReturnsCreated()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/register", NewRegisterRequest("Participant"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        RegisterResponse? body = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(body);
        Assert.True(body!.UserId > 0);
        Assert.Equal("Participant", body.RoleName);
    }

    [Fact]
    public async Task Register_AsOrganiser_ReturnsCreated()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/register", NewRegisterRequest("Organiser"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        RegisterResponse? body = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.Equal("Organiser", body!.RoleName);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        HttpClient client = _factory.CreateClient();
        string email = $"duplicate.{Guid.NewGuid():N}@test.co.za";

        HttpResponseMessage first = await client.PostAsJsonAsync("/api/auth/register", NewRegisterRequest(email: email));
        HttpResponseMessage second = await client.PostAsJsonAsync("/api/auth/register", NewRegisterRequest(email: email.ToUpper()));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidEmail_ReturnsBadRequest()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/register", NewRegisterRequest(email: "not-an-email"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_MissingPassword_ReturnsBadRequest()
    {
        HttpClient client = _factory.CreateClient();
        var body = new
        {
            firstName = "Sipho",
            lastName = "Khumalo",
            email = $"nopassword.{Guid.NewGuid():N}@test.co.za",
            roleName = "Participant"
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/register", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_PasswordShorterThan8Characters_ReturnsBadRequest()
    {
        HttpClient client = _factory.CreateClient();
        RegisterRequest request = NewRegisterRequest();
        request.Password = "Short1!";

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_UnknownRole_ReturnsBadRequest()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/register", NewRegisterRequest("Admin"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_DateOfBirthInTheFuture_ReturnsBadRequest()
    {
        HttpClient client = _factory.CreateClient();
        RegisterRequest request = NewRegisterRequest();
        request.DateOfBirth = DateOnly.FromDateTime(DateTime.Today.AddDays(5));

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_SavesThePasswordAsAHash()
    {
        HttpClient client = _factory.CreateClient();
        RegisterRequest request = NewRegisterRequest();

        await client.PostAsJsonAsync("/api/auth/register", request);

        using IServiceScope scope = _factory.Services.CreateScope();
        RaceDayDbContext db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        string savedEmail = request.Email.ToLower();
        var user = db.Users.Single(u => u.Email == savedEmail);

        Assert.NotEqual(request.Password, user.PasswordHash);
        Assert.DoesNotContain(request.Password, user.PasswordHash);
        Assert.True(PasswordHasher.Verify(request.Password, user.PasswordHash));
    }

    [Theory]
    [InlineData("Organiser")]
    [InlineData("Participant")]
    public async Task Login_CorrectCredentials_ReturnsOkWithTheRole(string roleName)
    {
        HttpClient client = _factory.CreateClient();
        RegisterRequest register = NewRegisterRequest(roleName);
        await client.PostAsJsonAsync("/api/auth/register", register);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = register.Email, Password = register.Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        LoginResponse? body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.Equal(roleName, body!.RoleName);
        Assert.True(body.UserId > 0);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        HttpClient client = _factory.CreateClient();
        RegisterRequest register = NewRegisterRequest();
        await client.PostAsJsonAsync("/api/auth/register", register);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = register.Email, Password = "WrongPassword1!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_ReturnsUnauthorized()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "nobody@test.co.za", Password = TestHelpers.Password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_MissingPassword_ReturnsBadRequest()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/login", new { email = "someone@test.co.za" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_EmailIsNotCaseSensitive()
    {
        HttpClient client = _factory.CreateClient();
        RegisterRequest register = NewRegisterRequest();
        await client.PostAsJsonAsync("/api/auth/register", register);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = register.Email.ToUpper(), Password = register.Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutLogin_ReturnsUnauthorized()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Session_KeepsTheUserLoggedInForTheNextRequests()
    {
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);

        HttpResponseMessage first = await participant.Client.GetAsync("/api/users/me");
        HttpResponseMessage second = await participant.Client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        UserResponse? profile = await second.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal(participant.UserId, profile!.UserId);
        Assert.Equal("Participant", profile.RoleName);
    }

    [Fact]
    public async Task Session_IsSeparateForEachClient()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);

        UserResponse? organiserProfile = await organiser.Client.GetFromJsonAsync<UserResponse>("/api/users/me");
        UserResponse? participantProfile = await participant.Client.GetFromJsonAsync<UserResponse>("/api/users/me");

        Assert.Equal("Organiser", organiserProfile!.RoleName);
        Assert.Equal("Participant", participantProfile!.RoleName);
        Assert.NotEqual(organiserProfile.UserId, participantProfile.UserId);
    }

    [Fact]
    public async Task Logout_EndsTheSession()
    {
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);

        HttpResponseMessage logout = await participant.Client.PostAsync("/api/auth/logout", null);
        HttpResponseMessage afterLogout = await participant.Client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutLogin_ReturnsUnauthorized()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}