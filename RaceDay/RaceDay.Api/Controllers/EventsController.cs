using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
[Produces("application/json")]
public class EventsController : BaseApiController
{
    private readonly RaceDayDbContext _db;

    public EventsController(RaceDayDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [SessionAuthorize]
    [ProducesResponseType(typeof(List<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetEvents(
        [FromQuery] string? search,
        [FromQuery] string? eventType,
        [FromQuery] string? province,
        [FromQuery] bool upcomingOnly = false)
    {
        IQueryable<Event> query = _db.Events.Include(e => e.Organiser);

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            string? normalisedType = EventTypes.Normalise(eventType);
            if (normalisedType == null)
            {
                return BadRequest(new { message = "The event type must be Running, Walking or Cycling." });
            }

            query = query.Where(e => e.EventType == normalisedType);
        }

        if (!string.IsNullOrWhiteSpace(province))
        {
            string provinceText = province.Trim().ToLower();
            query = query.Where(e => e.Province.ToLower() == provinceText);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim().ToLower();
            query = query.Where(e => e.Name.ToLower().Contains(term)
                || e.Location.ToLower().Contains(term)
                || (e.Description != null && e.Description.ToLower().Contains(term)));
        }

        if (upcomingOnly)
        {
            DateTime now = DateTime.Now;
            query = query.Where(e => e.EventDate > now);
        }

        List<Event> events = await query.OrderBy(e => e.EventDate).ToListAsync();

        List<EventResponse> responses = events.Select(ToResponse).ToList();
        await AddEnrolmentCountsAsync(responses);

        return Ok(responses);
    }

    [HttpGet("{id:int}")]
    [SessionAuthorize]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEvent(int id)
    {
        Event? ev = await _db.Events.Include(e => e.Organiser).FirstOrDefaultAsync(e => e.EventId == id);
        if (ev == null)
        {
            return NotFound(new { message = "That event does not exist." });
        }

        EventResponse response = ToResponse(ev);
        await AddEnrolmentCountsAsync(new List<EventResponse> { response });

        return Ok(response);
    }

    [HttpGet("my-events")]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(typeof(List<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyEvents()
    {
        int organiserId = CurrentUserId;

        List<Event> events = await _db.Events
            .Include(e => e.Organiser)
            .Where(e => e.OrganiserId == organiserId)
            .OrderBy(e => e.EventDate)
            .ToListAsync();

        List<EventResponse> responses = events.Select(ToResponse).ToList();
        await AddEnrolmentCountsAsync(responses);

        return Ok(responses);
    }

    [HttpPost]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateEvent([FromBody] EventRequest request)
    {
        string? eventType = EventTypes.Normalise(request.EventType);
        if (eventType == null)
        {
            return BadRequest(new { message = "The event type must be Running, Walking or Cycling." });
        }

        if (request.EventDate <= DateTime.Now)
        {
            return BadRequest(new { message = "The event date must be in the future." });
        }

        User? organiser = await _db.Users.FirstOrDefaultAsync(u => u.UserId == CurrentUserId);
        if (organiser == null)
        {
            HttpContext.Session.Clear();
            return Unauthorized(new { message = "You must log in first." });
        }

        var ev = new Event
        {
            Organiser = organiser,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            EventDate = request.EventDate,
            Location = request.Location.Trim(),
            Province = request.Province.Trim(),
            EventType = eventType,
            DistanceKm = request.DistanceKm,
            Latitude = request.Latitude,
            Longitude = request.Longitude
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEvent), new { id = ev.EventId }, ToResponse(ev));
    }

    [HttpPut("{id:int}")]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvent(int id, [FromBody] EventRequest request)
    {
        Event? ev = await _db.Events.Include(e => e.Organiser).FirstOrDefaultAsync(e => e.EventId == id);
        if (ev == null)
        {
            return NotFound(new { message = "That event does not exist." });
        }

        if (ev.OrganiserId != CurrentUserId)
        {
            return Forbidden("You can only update your own events.");
        }

        string? eventType = EventTypes.Normalise(request.EventType);
        if (eventType == null)
        {
            return BadRequest(new { message = "The event type must be Running, Walking or Cycling." });
        }

        if (request.EventDate != ev.EventDate && request.EventDate <= DateTime.Now)
        {
            return BadRequest(new { message = "The new event date must be in the future." });
        }

        ev.Name = request.Name.Trim();
        ev.Description = request.Description?.Trim();
        ev.EventDate = request.EventDate;
        ev.Location = request.Location.Trim();
        ev.Province = request.Province.Trim();
        ev.EventType = eventType;
        ev.DistanceKm = request.DistanceKm;
        ev.Latitude = request.Latitude;
        ev.Longitude = request.Longitude;

        await _db.SaveChangesAsync();

        EventResponse response = ToResponse(ev);
        await AddEnrolmentCountsAsync(new List<EventResponse> { response });

        return Ok(response);
    }

    [HttpDelete("{id:int}")]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        Event? ev = await _db.Events
            .Include(e => e.Categories)
                .ThenInclude(c => c.Enrolments)
                    .ThenInclude(en => en.Result)
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (ev == null)
        {
            return NotFound(new { message = "That event does not exist." });
        }

        if (ev.OrganiserId != CurrentUserId)
        {
            return Forbidden("You can only delete your own events.");
        }

        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private async Task AddEnrolmentCountsAsync(List<EventResponse> responses)
    {
        if (responses.Count == 0)
        {
            return;
        }

        List<int> eventIds = responses.Select(r => r.EventId).ToList();

        Dictionary<int, int> counts = await _db.Enrolments
            .Where(en => eventIds.Contains(en.EventId) && en.Status == EnrolmentStatuses.Confirmed)
            .GroupBy(en => en.EventId)
            .Select(g => new { EventId = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.EventId, x => x.Total);

        foreach (EventResponse response in responses)
        {
            response.EnrolmentCount = counts.TryGetValue(response.EventId, out int total) ? total : 0;
        }
    }

    private static EventResponse ToResponse(Event ev)
    {
        return new EventResponse
        {
            EventId = ev.EventId,
            OrganiserId = ev.OrganiserId,
            OrganiserName = $"{ev.Organiser.FirstName} {ev.Organiser.LastName}",
            Name = ev.Name,
            Description = ev.Description,
            EventDate = ev.EventDate,
            Location = ev.Location,
            Province = ev.Province,
            EventType = ev.EventType,
            DistanceKm = ev.DistanceKm,
            Latitude = ev.Latitude,
            Longitude = ev.Longitude,
            BannerImageUrl = ev.BannerImageUrl,
            CreatedAt = ev.CreatedAt
        };
    }
}