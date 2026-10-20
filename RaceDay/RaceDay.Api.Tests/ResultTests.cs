namespace RaceDay.Api.Tests;

public class ResultTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public ResultTests(RaceDayFactory factory)
    {
        _factory = factory;
    }
    private async Task<(TestUser Organiser, TestUser Participant, EventResponse Event, EnrolmentResponse Enrolment)> SetUpRaceAsync()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);
        var (ev, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);
        EnrolmentResponse enrolment = await TestHelpers.EnrolAsync(participant, category.CategoryId);

        return (organiser, participant, ev, enrolment);
    }

    [Fact]
    public async Task CaptureResult_AsOrganiser_ReturnsCreated()
    {
        var (organiser, participant, ev, enrolment) = await SetUpRaceAsync();

        HttpResponseMessage response = await organiser.Client.PostAsJsonAsync(
            "/api/results", TestHelpers.ValidResultRequest(enrolment.EnrolmentId, 1, new TimeSpan(0, 47, 32)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        ResultResponse? result = await response.Content.ReadFromJsonAsync<ResultResponse>();
        Assert.Equal(enrolment.EnrolmentId, result!.EnrolmentId);
        Assert.Equal(new TimeSpan(0, 47, 32), result.FinishTime);
        Assert.Equal(1, result.PositionOverall);
        Assert.Equal("Finished", result.Status);
        Assert.Equal(participant.UserId, result.ParticipantId);
        Assert.Equal(enrolment.RaceNumber, result.RaceNumber);
        Assert.Equal(ev.EventId, result.EventId);
    }

    [Fact]
    public async Task CaptureResult_AsParticipant_ReturnsForbidden()
    {
        var (_, participant, _, enrolment) = await SetUpRaceAsync();

        HttpResponseMessage response = await participant.Client.PostAsJsonAsync(
            "/api/results", TestHelpers.ValidResultRequest(enrolment.EnrolmentId));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_WithoutLogin_ReturnsUnauthorized()
    {
        HttpClient anonymous = _factory.CreateClient();

        HttpResponseMessage response = await anonymous.PostAsJsonAsync("/api/results", TestHelpers.ValidResultRequest(1));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_ForAnotherOrganisersEvent_ReturnsForbidden()
    {
        var (_, _, _, enrolment) = await SetUpRaceAsync();
        TestUser otherOrganiser = await TestHelpers.NewOrganiserAsync(_factory);

        HttpResponseMessage response = await otherOrganiser.Client.PostAsJsonAsync(
            "/api/results", TestHelpers.ValidResultRequest(enrolment.EnrolmentId));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_UnknownEnrolment_ReturnsNotFound()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);

        HttpResponseMessage response = await organiser.Client.PostAsJsonAsync(
            "/api/results", TestHelpers.ValidResultRequest(999999));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_SecondResultForTheSameEnrolment_ReturnsConflict()
    {
        var (organiser, _, _, enrolment) = await SetUpRaceAsync();
        await TestHelpers.CaptureResultAsync(organiser, enrolment.EnrolmentId);

        HttpResponseMessage response = await organiser.Client.PostAsJsonAsync(
            "/api/results", TestHelpers.ValidResultRequest(enrolment.EnrolmentId, 2));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_FinishedWithoutAFinishTime_ReturnsBadRequest()
    {
        var (organiser, _, _, enrolment) = await SetUpRaceAsync();
        CreateResultRequest request = TestHelpers.ValidResultRequest(enrolment.EnrolmentId);
        request.FinishTime = null;

        HttpResponseMessage response = await organiser.Client.PostAsJsonAsync("/api/results", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(24)]
    public async Task CaptureResult_FinishTimeOfZeroOr24Hours_ReturnsBadRequest(int hours)
    {
        var (organiser, _, _, enrolment) = await SetUpRaceAsync();
        CreateResultRequest request = TestHelpers.ValidResultRequest(enrolment.EnrolmentId, 1, TimeSpan.FromHours(hours));

        HttpResponseMessage response = await organiser.Client.PostAsJsonAsync("/api/results", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_PositionOfZero_ReturnsBadRequest()
    {
        var (organiser, _, _, enrolment) = await SetUpRaceAsync();
        CreateResultRequest request = TestHelpers.ValidResultRequest(enrolment.EnrolmentId, 0);

        HttpResponseMessage response = await organiser.Client.PostAsJsonAsync("/api/results", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_UnknownStatus_ReturnsBadRequest()
    {
        var (organiser, _, _, enrolment) = await SetUpRaceAsync();
        CreateResultRequest request = TestHelpers.ValidResultRequest(enrolment.EnrolmentId);
        request.Status = "Winner";

        HttpResponseMessage response = await organiser.Client.PostAsJsonAsync("/api/results", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_DidNotFinishWithoutATime_ReturnsCreated()
    {
        var (organiser, _, _, enrolment) = await SetUpRaceAsync();
        var request = new CreateResultRequest { EnrolmentId = enrolment.EnrolmentId, Status = "dnf" };

        HttpResponseMessage response = await organiser.Client.PostAsJsonAsync("/api/results", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        ResultResponse? result = await response.Content.ReadFromJsonAsync<ResultResponse>();
        Assert.Equal("DNF", result!.Status);
        Assert.Null(result.FinishTime);
        Assert.Null(result.PositionOverall);
    }

    [Fact]
    public async Task CaptureResult_DidNotStartWithAFinishTime_ReturnsBadRequest()
    {
        var (organiser, _, _, enrolment) = await SetUpRaceAsync();
        CreateResultRequest request = TestHelpers.ValidResultRequest(enrolment.EnrolmentId);
        request.Status = "DNS";

        HttpResponseMessage response = await organiser.Client.PostAsJsonAsync("/api/results", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_ForACancelledEnrolment_ReturnsConflict()
    {
        var (organiser, participant, _, enrolment) = await SetUpRaceAsync();
        await participant.Client.DeleteAsync($"/api/enrolments/{enrolment.EnrolmentId}");

        HttpResponseMessage response = await organiser.Client.PostAsJsonAsync(
            "/api/results", TestHelpers.ValidResultRequest(enrolment.EnrolmentId));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UpdateResult_OwnEvent_ReturnsTheUpdatedResult()
    {
        var (organiser, participant, _, enrolment) = await SetUpRaceAsync();
        ResultResponse result = await TestHelpers.CaptureResultAsync(organiser, enrolment.EnrolmentId);
        var update = new UpdateResultRequest { FinishTime = new TimeSpan(0, 45, 10), PositionOverall = 2, Status = "Finished" };

        HttpResponseMessage response = await organiser.Client.PutAsJsonAsync($"/api/results/{result.ResultId}", update);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        List<ResultResponse>? mine = await participant.Client.GetFromJsonAsync<List<ResultResponse>>("/api/results/me");
        ResultResponse saved = Assert.Single(mine!);
        Assert.Equal(new TimeSpan(0, 45, 10), saved.FinishTime);
        Assert.Equal(2, saved.PositionOverall);
    }

    [Fact]
    public async Task UpdateResult_AnotherOrganisersEvent_ReturnsForbidden()
    {
        var (organiser, _, _, enrolment) = await SetUpRaceAsync();
        ResultResponse result = await TestHelpers.CaptureResultAsync(organiser, enrolment.EnrolmentId);
        TestUser otherOrganiser = await TestHelpers.NewOrganiserAsync(_factory);
        var update = new UpdateResultRequest { FinishTime = new TimeSpan(0, 30, 0), PositionOverall = 1, Status = "Finished" };

        HttpResponseMessage response = await otherOrganiser.Client.PutAsJsonAsync($"/api/results/{result.ResultId}", update);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateResult_AsParticipant_ReturnsForbidden()
    {
        var (organiser, participant, _, enrolment) = await SetUpRaceAsync();
        ResultResponse result = await TestHelpers.CaptureResultAsync(organiser, enrolment.EnrolmentId);
        var update = new UpdateResultRequest { FinishTime = new TimeSpan(0, 1, 0), PositionOverall = 1, Status = "Finished" };

        HttpResponseMessage response = await participant.Client.PutAsJsonAsync($"/api/results/{result.ResultId}", update);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateResult_UnknownId_ReturnsNotFound()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        var update = new UpdateResultRequest { FinishTime = new TimeSpan(0, 30, 0), PositionOverall = 1, Status = "Finished" };

        HttpResponseMessage response = await organiser.Client.PutAsJsonAsync("/api/results/999999", update);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateResult_InvalidFinishTime_ReturnsBadRequest()
    {
        var (organiser, _, _, enrolment) = await SetUpRaceAsync();
        ResultResponse result = await TestHelpers.CaptureResultAsync(organiser, enrolment.EnrolmentId);
        var update = new UpdateResultRequest { FinishTime = TimeSpan.Zero, PositionOverall = 1, Status = "Finished" };

        HttpResponseMessage response = await organiser.Client.PutAsJsonAsync($"/api/results/{result.ResultId}", update);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteResult_OwnEvent_ReturnsNoContentAndTheParticipantNoLongerSeesIt()
    {
        var (organiser, participant, _, enrolment) = await SetUpRaceAsync();
        ResultResponse result = await TestHelpers.CaptureResultAsync(organiser, enrolment.EnrolmentId);

        HttpResponseMessage response = await organiser.Client.DeleteAsync($"/api/results/{result.ResultId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        List<ResultResponse>? mine = await participant.Client.GetFromJsonAsync<List<ResultResponse>>("/api/results/me");
        Assert.Empty(mine!);
    }

    [Fact]
    public async Task DeleteResult_AnotherOrganisersEvent_ReturnsForbidden()
    {
        var (organiser, _, _, enrolment) = await SetUpRaceAsync();
        ResultResponse result = await TestHelpers.CaptureResultAsync(organiser, enrolment.EnrolmentId);
        TestUser otherOrganiser = await TestHelpers.NewOrganiserAsync(_factory);

        HttpResponseMessage response = await otherOrganiser.Client.DeleteAsync($"/api/results/{result.ResultId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteResult_AsParticipant_ReturnsForbidden()
    {
        var (organiser, participant, _, enrolment) = await SetUpRaceAsync();
        ResultResponse result = await TestHelpers.CaptureResultAsync(organiser, enrolment.EnrolmentId);

        HttpResponseMessage response = await participant.Client.DeleteAsync($"/api/results/{result.ResultId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteResult_UnknownId_ReturnsNotFound()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);

        HttpResponseMessage response = await organiser.Client.DeleteAsync("/api/results/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMyResults_AsParticipant_ReturnsOnlyTheirOwnResults()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        TestUser first = await TestHelpers.NewParticipantAsync(_factory);
        TestUser second = await TestHelpers.NewParticipantAsync(_factory);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);
        EnrolmentResponse firstEnrolment = await TestHelpers.EnrolAsync(first, category.CategoryId);
        EnrolmentResponse secondEnrolment = await TestHelpers.EnrolAsync(second, category.CategoryId);
        await TestHelpers.CaptureResultAsync(organiser, firstEnrolment.EnrolmentId, 1);
        await TestHelpers.CaptureResultAsync(organiser, secondEnrolment.EnrolmentId, 2);

        List<ResultResponse>? mine = await first.Client.GetFromJsonAsync<List<ResultResponse>>("/api/results/me");

        ResultResponse result = Assert.Single(mine!);
        Assert.Equal(first.UserId, result.ParticipantId);
        Assert.Equal(1, result.PositionOverall);
    }

    [Fact]
    public async Task GetMyResults_AsOrganiser_ReturnsForbidden()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);

        HttpResponseMessage response = await organiser.Client.GetAsync("/api/results/me");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetMyResults_WithoutLogin_ReturnsUnauthorized()
    {
        HttpClient anonymous = _factory.CreateClient();

        HttpResponseMessage response = await anonymous.GetAsync("/api/results/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetResultsForEvent_AsParticipant_ReturnsTheResultsInPositionOrder()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        var (ev, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);

        TestUser third = await TestHelpers.NewParticipantAsync(_factory);
        TestUser winner = await TestHelpers.NewParticipantAsync(_factory);
        TestUser second = await TestHelpers.NewParticipantAsync(_factory);
        EnrolmentResponse thirdEnrolment = await TestHelpers.EnrolAsync(third, category.CategoryId);
        EnrolmentResponse winnerEnrolment = await TestHelpers.EnrolAsync(winner, category.CategoryId);
        EnrolmentResponse secondEnrolment = await TestHelpers.EnrolAsync(second, category.CategoryId);
        await TestHelpers.CaptureResultAsync(organiser, thirdEnrolment.EnrolmentId, 3);
        await TestHelpers.CaptureResultAsync(organiser, winnerEnrolment.EnrolmentId, 1);
        await TestHelpers.CaptureResultAsync(organiser, secondEnrolment.EnrolmentId, 2);

        HttpResponseMessage response = await third.Client.GetAsync($"/api/events/{ev.EventId}/results");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<ResultResponse>? results = await response.Content.ReadFromJsonAsync<List<ResultResponse>>();
        Assert.Equal(3, results!.Count);
        Assert.Equal(new int?[] { 1, 2, 3 }, results.Select(r => r.PositionOverall).ToArray());
        Assert.Equal(winner.UserId, results[0].ParticipantId);
    }

    [Fact]
    public async Task GetResultsForEvent_AsOrganiser_ReturnsOk()
    {
        TestUser organiser = await TestHelpers.NewOrganiserAsync(_factory);
        EventResponse ev = await TestHelpers.CreateEventAsync(organiser);

        HttpResponseMessage response = await organiser.Client.GetAsync($"/api/events/{ev.EventId}/results");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetResultsForEvent_WithoutLogin_ReturnsUnauthorized()
    {
        HttpClient anonymous = _factory.CreateClient();

        HttpResponseMessage response = await anonymous.GetAsync("/api/events/1/results");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetResultsForEvent_UnknownEvent_ReturnsNotFound()
    {
        TestUser participant = await TestHelpers.NewParticipantAsync(_factory);

        HttpResponseMessage response = await participant.Client.GetAsync("/api/events/999999/results");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}