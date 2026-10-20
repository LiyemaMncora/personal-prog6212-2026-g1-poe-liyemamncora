using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
[Produces("application/json")]
public class ResultsController : BaseApiController
{
    private readonly RaceDayDbContext _db;

    public ResultsController(RaceDayDbContext db)
    {
        _db = db;
    }

    [HttpPost("results")]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateResult([FromBody] CreateResultRequest request)
    {
        string? problem = CheckResultFields(request.Status, request.FinishTime, request.PositionOverall, out string status);
        if (problem != null)
        {
            return BadRequest(new { message = problem });
        }

        Enrolment? enrolment = await _db.Enrolments
            .Include(e => e.Category)
                .ThenInclude(c => c.Event)
            .Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.EnrolmentId == request.EnrolmentId);
        if (enrolment == null)
        {
            return NotFound(new { message = "That enrolment does not exist." });
        }

        if (enrolment.Category.Event.OrganiserId != CurrentUserId)
        {
            return Forbidden("You can only capture results for your own events.");
        }

        if (enrolment.Result != null)
        {
            return Conflict(new { message = "This enrolment already has a result. Update the existing result instead." });
        }

        if (enrolment.Status == EnrolmentStatuses.Cancelled)
        {
            return Conflict(new { message = "This enrolment was cancelled, so it cannot have a result." });
        }

        var result = new Result
        {
            EnrolmentId = enrolment.EnrolmentId,
            FinishTime = TimeHelper.ToWholeSeconds(request.FinishTime),
            PositionOverall = request.PositionOverall,
            Status = status
        };

        _db.Results.Add(result);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "This enrolment already has a result. Update the existing result instead." });
        }

        Result saved = (await WithDetails().FirstOrDefaultAsync(r => r.ResultId == result.ResultId))!;

        return CreatedAtAction(nameof(GetResultsForEvent), new { eventId = saved.Enrolment.Category.EventId }, ToResponse(saved));
    }

    [HttpPut("results/{id:int}")]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateResult(int id, [FromBody] UpdateResultRequest request)
    {
        Result? result = await WithDetails().FirstOrDefaultAsync(r => r.ResultId == id);
        if (result == null)
        {
            return NotFound(new { message = "That result does not exist." });
        }

        if (result.Enrolment.Category.Event.OrganiserId != CurrentUserId)
        {
            return Forbidden("You can only update the results of your own events.");
        }

        string? problem = CheckResultFields(request.Status, request.FinishTime, request.PositionOverall, out string status);
        if (problem != null)
        {
            return BadRequest(new { message = problem });
        }

        result.FinishTime = TimeHelper.ToWholeSeconds(request.FinishTime);
        result.PositionOverall = request.PositionOverall;
        result.Status = status;

        await _db.SaveChangesAsync();

        return Ok(ToResponse(result));
    }

    [HttpDelete("results/{id:int}")]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteResult(int id)
    {
        Result? result = await WithDetails().FirstOrDefaultAsync(r => r.ResultId == id);
        if (result == null)
        {
            return NotFound(new { message = "That result does not exist." });
        }

        if (result.Enrolment.Category.Event.OrganiserId != CurrentUserId)
        {
            return Forbidden("You can only delete the results of your own events.");
        }

        _db.Results.Remove(result);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("results/me")]
    [SessionAuthorize(RoleNames.Participant)]
    [ProducesResponseType(typeof(List<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyResults()
    {
        int participantId = CurrentUserId;

        List<Result> results = await WithDetails()
            .Where(r => r.Enrolment.ParticipantId == participantId)
            .OrderByDescending(r => r.Enrolment.Category.Event.EventDate)
            .ToListAsync();

        return Ok(results.Select(ToResponse).ToList());
    }

    [HttpGet("events/{eventId:int}/results")]
    [SessionAuthorize]
    [ProducesResponseType(typeof(List<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetResultsForEvent(int eventId)
    {
        bool eventExists = await _db.Events.AnyAsync(e => e.EventId == eventId);
        if (!eventExists)
        {
            return NotFound(new { message = "That event does not exist." });
        }

        List<Result> results = await WithDetails()
            .Where(r => r.Enrolment.Category.EventId == eventId)
            .OrderBy(r => r.Enrolment.CategoryId)
            .ThenBy(r => r.PositionOverall ?? int.MaxValue)
            .ThenBy(r => r.FinishTime)
            .ToListAsync();

        return Ok(results.Select(ToResponse).ToList());
    }

    private IQueryable<Result> WithDetails()
    {
        return _db.Results
            .Include(r => r.Enrolment)
                .ThenInclude(e => e.Participant)
            .Include(r => r.Enrolment)
                .ThenInclude(e => e.Category)
                    .ThenInclude(c => c.Event);
    }

    private static string? CheckResultFields(string status, TimeSpan? finishTime, int? positionOverall, out string normalisedStatus)
    {
        string? cleanStatus = ResultStatuses.Normalise(status);
        if (cleanStatus == null)
        {
            normalisedStatus = string.Empty;
            return "The status must be Finished, DNF or DNS.";
        }

        normalisedStatus = cleanStatus;

        if (cleanStatus == ResultStatuses.Finished)
        {
            if (finishTime == null || !TimeHelper.IsValidFinishTime(finishTime.Value))
            {
                return "A finished result needs a finish time of more than 00:00:00 and less than 24 hours.";
            }
        }
        else if (finishTime != null || positionOverall != null)
        {
            return "A DNF or DNS result cannot have a finish time or a position.";
        }

        return null;
    }

    private static ResultResponse ToResponse(Result result)
    {
        Enrolment enrolment = result.Enrolment;

        return new ResultResponse
        {
            ResultId = result.ResultId,
            EnrolmentId = result.EnrolmentId,
            FinishTime = result.FinishTime,
            PositionOverall = result.PositionOverall,
            Status = result.Status,
            CapturedAt = result.CapturedAt,
            ParticipantId = enrolment.ParticipantId,
            ParticipantName = $"{enrolment.Participant.FirstName} {enrolment.Participant.LastName}",
            RaceNumber = enrolment.RaceNumber,
            CategoryId = enrolment.CategoryId,
            CategoryName = enrolment.Category.Name,
            DistanceKm = enrolment.Category.DistanceKm,
            EventId = enrolment.Category.EventId,
            EventName = enrolment.Category.Event.Name,
            EventDate = enrolment.Category.Event.EventDate
        };
    }
}