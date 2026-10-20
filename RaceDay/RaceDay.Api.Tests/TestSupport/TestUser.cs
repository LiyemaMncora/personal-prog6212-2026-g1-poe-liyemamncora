namespace RaceDay.Api.Tests.TestSupport;

public record TestUser(HttpClient Client, int UserId, string Email);