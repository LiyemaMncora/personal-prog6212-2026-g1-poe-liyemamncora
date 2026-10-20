namespace RaceDay.Api.Tests;

public class RoleTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public RoleTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetRoles_WithoutLogin_ReturnsOrganiserAndParticipant()
    {
        HttpClient anonymous = _factory.CreateClient();

        HttpResponseMessage response = await anonymous.GetAsync("/api/roles");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<RoleResponse>? roles = await response.Content.ReadFromJsonAsync<List<RoleResponse>>();
        Assert.Equal(2, roles!.Count);
        Assert.Contains(roles, r => r.RoleName == "Organiser");
        Assert.Contains(roles, r => r.RoleName == "Participant");
    }

    [Fact]
    public async Task GetRoles_AsLoggedInUser_ReturnsTheSameRoles()
    {
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);

        List<RoleResponse>? roles = await participant.Client.GetFromJsonAsync<List<RoleResponse>>("/api/roles");

        Assert.Equal(2, roles!.Count);
    }

    [Fact]
    public async Task EveryListedRole_CanBeUsedToRegister()
    {
        HttpClient anonymous = _factory.CreateClient();
        List<RoleResponse>? roles = await anonymous.GetFromJsonAsync<List<RoleResponse>>("/api/roles");

        foreach (RoleResponse role in roles!)
        {
            TestUser user = await TestHelpers.RegisterAndLoginAsync(_factory, role.RoleName);

            UserResponse? profile = await user.Client.GetFromJsonAsync<UserResponse>("/api/users/me");
            Assert.Equal(role.RoleName, profile!.RoleName);
        }
    }
}