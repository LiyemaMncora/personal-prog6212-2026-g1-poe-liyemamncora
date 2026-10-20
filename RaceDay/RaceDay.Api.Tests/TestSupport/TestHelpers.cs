using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Tests.TestSupport;

public static class TestHelpers
{
    public const string Password = "Test@12345";

    public static async Task<TestUser> RegisterAndLoginAsync(RaceDayFactory factory, string roleName)
    {
        HttpClient client = factory.CreateClient();
        string email = $"{roleName.ToLower()}.{Guid.NewGuid():N}@test.co.za";

        var register = new RegisterRequest
        {
            FirstName = "Test",
            LastName = roleName,
            Email = email,
            Password = Password,
            PhoneNumber = "0710000000",
            DateOfBirth = new DateOnly(1995, 5, 5),
            RoleName = roleName
        };

        HttpResponseMessage registerResponse = await client.PostAsJsonAsync("/api/auth/register", register);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        HttpResponseMessage loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = Password });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        LoginResponse? login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        return new TestUser(client, login!.UserId, email);
    }

    public static Task<TestUser> NewOrganiserAsync(RaceDayFactory factory)
    {
        return RegisterAndLoginAsync(factory, "Organiser");
    }

    public static Task<TestUser> NewParticipantAsync(RaceDayFactory factory)
    {
        return RegisterAndLoginAsync(factory, "Participant");
    }


    public static EventRequest ValidEventRequest(string eventType = "Running")
    {
        return new EventRequest
        {
            Name = $"Test Race {Guid.NewGuid():N}",
            Description = "A race that is created by a test.",
            EventDate = DateTime.Now.AddDays(30),
            Location = "Durban",
            Province = "KwaZulu-Natal",
            EventType = eventType,
            DistanceKm = 10m,
            Latitude = -29.8673m,
            Longitude = 31.0454m
        };
    }

    public static async Task<EventResponse> CreateEventAsync(TestUser organiser)
    {
        HttpResponseMessage response = await organiser.Client.PostAsJsonAsync("/api/events", ValidEventRequest());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return (await response.Content.ReadFromJsonAsync<EventResponse>())!;
    }

    public static CategoryRequest ValidCategoryRequest(string name = "10km Run", decimal distanceKm = 10, int? maxParticipants = null)
    {
        return new CategoryRequest
        {
            Name = name,
            DistanceKm = distanceKm,
            EntryFee = 120,
            MaxParticipants = maxParticipants,
            StartTime = new TimeSpan(6, 0, 0)
        };
    }

    public static async Task<CategoryResponse> CreateCategoryAsync(TestUser organiser, int eventId, string name = "10km Run", decimal distanceKm = 10, int? maxParticipants = null)
    {
        HttpResponseMessage response = await organiser.Client.PostAsJsonAsync(
            $"/api/events/{eventId}/categories", ValidCategoryRequest(name, distanceKm, maxParticipants));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return (await response.Content.ReadFromJsonAsync<CategoryResponse>())!;
    }

    public static async Task<(EventResponse Event, CategoryResponse Category)> CreateEventWithCategoryAsync(TestUser organiser, int? maxParticipants = null)
    {
        EventResponse ev = await CreateEventAsync(organiser);
        CategoryResponse category = await CreateCategoryAsync(organiser, ev.EventId, maxParticipants: maxParticipants);

        return (ev, category);
    }


    public static async Task<EnrolmentResponse> EnrolAsync(TestUser participant, int categoryId)
    {
        HttpResponseMessage response = await participant.Client.PostAsJsonAsync("/api/enrolments", new CreateEnrolmentRequest { CategoryId = categoryId });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return (await response.Content.ReadFromJsonAsync<EnrolmentResponse>())!;
    }

    public static CreateResultRequest ValidResultRequest(int enrolmentId, int position = 1, TimeSpan? finishTime = null)
    {
        return new CreateResultRequest
        {
            EnrolmentId = enrolmentId,
            FinishTime = finishTime ?? new TimeSpan(0, 47, 32),
            PositionOverall = position,
            Status = "Finished"
        };
    }

    public static async Task<ResultResponse> CaptureResultAsync(TestUser organiser, int enrolmentId, int position = 1)
    {
        HttpResponseMessage response = await organiser.Client.PostAsJsonAsync("/api/results", ValidResultRequest(enrolmentId, position));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return (await response.Content.ReadFromJsonAsync<ResultResponse>())!;
    }

    public static async Task<(int EventId, int CategoryId, int EnrolmentId)> SeedPastEnrolmentAsync(RaceDayFactory factory, int organiserId, int participantId)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        RaceDayDbContext db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();

        var ev = new Event
        {
            OrganiserId = organiserId,
            Name = "Past Race",
            EventDate = DateTime.Now.AddDays(-10),
            Location = "Durban",
            Province = "KwaZulu-Natal",
            EventType = "Running",
            DistanceKm = 10
        };

        var category = new Category { Event = ev, Name = "10km Run", DistanceKm = 10, EntryFee = 100 };

        var enrolment = new Enrolment
        {
            Event = ev,
            Category = category,
            ParticipantId = participantId,
            RaceNumber = "X" + Guid.NewGuid().ToString("N").Substring(0, 8)
        };

        db.Enrolments.Add(enrolment);
        await db.SaveChangesAsync();

        return (ev.EventId, category.CategoryId, enrolment.EnrolmentId);
    }

    public static async Task<int> SeedPastCategoryAsync(RaceDayFactory factory, int organiserId)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        RaceDayDbContext db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();

        var ev = new Event
        {
            OrganiserId = organiserId,
            Name = "Old Race",
            EventDate = DateTime.Now.AddDays(-10),
            Location = "Paarl",
            Province = "Western Cape",
            EventType = "Cycling",
            DistanceKm = 25
        };

        var category = new Category { Event = ev, Name = "25km Cycle", DistanceKm = 25, EntryFee = 200 };

        db.Categories.Add(category);
        await db.SaveChangesAsync();

        return category.CategoryId;
    }

    public static async Task<int> GetStoredEventIdAsync(RaceDayFactory factory, int enrolmentId)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        RaceDayDbContext db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();

        Enrolment enrolment = await db.Enrolments.SingleAsync(e => e.EnrolmentId == enrolmentId);

        return enrolment.EventId;
    }
}