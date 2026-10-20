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
public class EnrolmentsController : BaseApiController
{
    private readonly RaceDayDbContext _db;

    public EnrolmentsController(RaceDayDbContext db)
    {
        _db = db;
    }

    [HttpPost("enrolments")]
    [SessionAuthorize(RoleNames.Participant)]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateEnrolment([FromBody] CreateEnrolmentRequest request)
    {
        int participantId = CurrentUserId;

        Category? category = await _db.Categories
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId);
        if (category == null)
        {
            return NotFound(new { message = "That category does not exist." });
        }

        if (category.Event.EventDate <= DateTime.Now)
        {
            return Conflict(new { message = "This event has already taken place, so it cannot be entered." });
        }

        Enrolment? existing = await _db.Enrolments
            .FirstOrDefaultAsync(e => e.CategoryId == category.CategoryId && e.ParticipantId == participantId);
        if (existing != null && existing.Status == EnrolmentStatuses.Confirmed)
        {
            return Conflict(new { message = "You are already entered in this category." });
        }

        int confirmed = await _db.Enrolments
            .CountAsync(e => e.CategoryId == category.CategoryId && e.Status == EnrolmentStatuses.Confirmed);
        if (category.MaxParticipants != null && confirmed >= category.MaxParticipants)
        {
            return Conflict(new { message = "This category is full." });
        }

        Enrolment enrolment;
        if (existing != null)
        {
            existing.Status = EnrolmentStatuses.Confirmed;
            existing.EnrolledAt = DateTime.Now;
            enrolment = existing;
        }
        else
        {
            enrolment = new Enrolment
            {
                EventId = category.EventId,
                CategoryId = category.CategoryId,
                ParticipantId = participantId,
                RaceNumber = await GenerateRaceNumberAsync(category.Event),
                Status = EnrolmentStatuses.Confirmed
            };
            _db.Enrolments.Add(enrolment);
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "The entry could not be saved because the category changed. Please try again." });
        }

        Enrolment saved = (await WithDetails().FirstOrDefaultAsync(e => e.EnrolmentId == enrolment.EnrolmentId))!;

        return CreatedAtAction(nameof(GetEnrolment), new { id = saved.EnrolmentId }, ToResponse(saved));
    }

    [HttpGet("enrolments/me")]
    [SessionAuthorize(RoleNames.Participant)]
    [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyEnrolments()
    {
        int participantId = CurrentUserId;

        List<Enrolment> enrolments = await WithDetails()
            .Where(e => e.ParticipantId == participantId)
            .OrderBy(e => e.Category.Event.EventDate)
            .ToListAsync();

        return Ok(enrolments.Select(ToResponse).ToList());
    }

    [HttpGet("enrolments/{id:int}")]
    [SessionAuthorize]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEnrolment(int id)
    {
        Enrolment? enrolment = await WithDetails().FirstOrDefaultAsync(e => e.EnrolmentId == id);
        if (enrolment == null)
        {
            return NotFound(new { message = "That enrolment does not exist." });
        }

        bool isOwner = enrolment.ParticipantId == CurrentUserId;
        bool isOrganiserOfEvent = CurrentRole == RoleNames.Organiser
            && enrolment.Category.Event.OrganiserId == CurrentUserId;

        if (!isOwner && !isOrganiserOfEvent)
        {
            return Forbidden("You can only see your own enrolments, or the enrolments of your own events.");
        }

        return Ok(ToResponse(enrolment));
    }

    [HttpDelete("enrolments/{id:int}")]
    [SessionAuthorize(RoleNames.Participant)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelEnrolment(int id)
    {
        Enrolment? enrolment = await WithDetails().FirstOrDefaultAsync(e => e.EnrolmentId == id);
        if (enrolment == null)
        {
            return NotFound(new { message = "That enrolment does not exist." });
        }

        if (enrolment.ParticipantId != CurrentUserId)
        {
            return Forbidden("You can only cancel your own enrolments.");
        }

        if (enrolment.Category.Event.EventDate <= DateTime.Now)
        {
            return Conflict(new { message = "This event has already taken place, so the enrolment cannot be cancelled." });
        }

        enrolment.Status = EnrolmentStatuses.Cancelled;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("events/{eventId:int}/enrolments")]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEnrolmentsForEvent(int eventId)
    {
        Event? ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev == null)
        {
            return NotFound(new { message = "That event does not exist." });
        }

        if (ev.OrganiserId != CurrentUserId)
        {
            return Forbidden("You can only see the enrolments of your own events.");
        }

        List<Enrolment> enrolments = await WithDetails()
            .Where(e => e.EventId == eventId)
            .OrderBy(e => e.RaceNumber)
            .ToListAsync();

        return Ok(enrolments.Select(ToResponse).ToList());
    }

    private IQueryable<Enrolment> WithDetails()
    {
        return _db.Enrolments
            .Include(e => e.Category)
                .ThenInclude(c => c.Event)
            .Include(e => e.Participant);
    }

    private async Task<string> GenerateRaceNumberAsync(Event ev)
    {
        string prefix = char.ToUpper(ev.Name.Trim()[0]).ToString() + ev.EventId;

        int next = await _db.Enrolments.CountAsync(e => e.EventId == ev.EventId) + 1;

        string raceNumber = prefix + next.ToString("000");
        while (await _db.Enrolments.AnyAsync(e => e.RaceNumber == raceNumber))
        {
            next++;
            raceNumber = prefix + next.ToString("000");
        }

        return raceNumber;
    }

    private static EnrolmentResponse ToResponse(Enrolment enrolment)
    {
        return new EnrolmentResponse
        {
            EnrolmentId = enrolment.EnrolmentId,
            RaceNumber = enrolment.RaceNumber,
            Status = enrolment.Status,
            EnrolledAt = enrolment.EnrolledAt,
            ParticipantId = enrolment.ParticipantId,
            ParticipantName = $"{enrolment.Participant.FirstName} {enrolment.Participant.LastName}",
            CategoryId = enrolment.CategoryId,
            CategoryName = enrolment.Category.Name,
            DistanceKm = enrolment.Category.DistanceKm,
            EntryFee = enrolment.Category.EntryFee,
            EventId = enrolment.EventId,
            EventName = enrolment.Category.Event.Name,
            EventDate = enrolment.Category.Event.EventDate,
            Location = enrolment.Category.Event.Location
        };
    }
}