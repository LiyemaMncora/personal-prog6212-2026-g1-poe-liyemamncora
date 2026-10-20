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
public class CategoriesController : BaseApiController
{
    private readonly RaceDayDbContext _db;

    public CategoriesController(RaceDayDbContext db)
    {
        _db = db;
    }

    [HttpGet("events/{eventId:int}/categories")]
    [SessionAuthorize]
    [ProducesResponseType(typeof(List<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoriesForEvent(int eventId)
    {
        bool eventExists = await _db.Events.AnyAsync(e => e.EventId == eventId);
        if (!eventExists)
        {
            return NotFound(new { message = "That event does not exist." });
        }

        List<CategoryResponse> categories = await ProjectToResponse(_db.Categories.Where(c => c.EventId == eventId))
            .OrderBy(c => c.DistanceKm)
            .ToListAsync();

        return Ok(categories);
    }

    [HttpGet("categories/{id:int}")]
    [SessionAuthorize]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategory(int id)
    {
        CategoryResponse? category = await ProjectToResponse(_db.Categories.Where(c => c.CategoryId == id))
            .FirstOrDefaultAsync();

        if (category == null)
        {
            return NotFound(new { message = "That category does not exist." });
        }

        return Ok(category);
    }

    [HttpPost("events/{eventId:int}/categories")]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCategory(int eventId, [FromBody] CategoryRequest request)
    {
        Event? ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev == null)
        {
            return NotFound(new { message = "That event does not exist." });
        }

        if (ev.OrganiserId != CurrentUserId)
        {
            return Forbidden("You can only add categories to your own events.");
        }

        if (request.StartTime != null && !TimeHelper.IsValidTimeOfDay(request.StartTime.Value))
        {
            return BadRequest(new { message = "The start time must be a time of the day, for example 06:00:00." });
        }

        string name = request.Name.Trim();
        bool nameUsed = await _db.Categories.AnyAsync(c => c.EventId == eventId && c.Name.ToLower() == name.ToLower());
        if (nameUsed)
        {
            return Conflict(new { message = "This event already has a category with that name." });
        }

        var category = new Category
        {
            EventId = eventId,
            Name = name,
            DistanceKm = request.DistanceKm,
            EntryFee = request.EntryFee,
            MaxParticipants = request.MaxParticipants,
            StartTime = TimeHelper.ToWholeSeconds(request.StartTime)
        };

        _db.Categories.Add(category);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "This event already has a category with that name." });
        }

        var response = new CategoryResponse
        {
            CategoryId = category.CategoryId,
            EventId = category.EventId,
            Name = category.Name,
            DistanceKm = category.DistanceKm,
            EntryFee = category.EntryFee,
            MaxParticipants = category.MaxParticipants,
            StartTime = category.StartTime,
            EnrolmentCount = 0
        };

        return CreatedAtAction(nameof(GetCategory), new { id = category.CategoryId }, response);
    }

    [HttpPut("categories/{id:int}")]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryRequest request)
    {
        Category? category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category == null)
        {
            return NotFound(new { message = "That category does not exist." });
        }

        if (category.Event.OrganiserId != CurrentUserId)
        {
            return Forbidden("You can only update the categories of your own events.");
        }

        if (request.StartTime != null && !TimeHelper.IsValidTimeOfDay(request.StartTime.Value))
        {
            return BadRequest(new { message = "The start time must be a time of the day, for example 06:00:00." });
        }

        string name = request.Name.Trim();
        bool nameUsed = await _db.Categories.AnyAsync(c =>
            c.EventId == category.EventId && c.CategoryId != id && c.Name.ToLower() == name.ToLower());
        if (nameUsed)
        {
            return Conflict(new { message = "This event already has a category with that name." });
        }

        int confirmed = await _db.Enrolments.CountAsync(e =>
            e.CategoryId == id && e.Status == EnrolmentStatuses.Confirmed);
        if (request.MaxParticipants != null && request.MaxParticipants < confirmed)
        {
            return Conflict(new { message = $"The limit cannot be lower than the {confirmed} participants that are already entered." });
        }

        category.Name = name;
        category.DistanceKm = request.DistanceKm;
        category.EntryFee = request.EntryFee;
        category.MaxParticipants = request.MaxParticipants;
        category.StartTime = TimeHelper.ToWholeSeconds(request.StartTime);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "This event already has a category with that name." });
        }

        var response = new CategoryResponse
        {
            CategoryId = category.CategoryId,
            EventId = category.EventId,
            Name = category.Name,
            DistanceKm = category.DistanceKm,
            EntryFee = category.EntryFee,
            MaxParticipants = category.MaxParticipants,
            StartTime = category.StartTime,
            EnrolmentCount = confirmed
        };

        return Ok(response);
    }

    [HttpDelete("categories/{id:int}")]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        Category? category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category == null)
        {
            return NotFound(new { message = "That category does not exist." });
        }

        if (category.Event.OrganiserId != CurrentUserId)
        {
            return Forbidden("You can only delete the categories of your own events.");
        }

        bool hasEnrolments = await _db.Enrolments.AnyAsync(e => e.CategoryId == id);
        if (hasEnrolments)
        {
            return Conflict(new { message = "Participants are already entered in this category, so it cannot be deleted." });
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static IQueryable<CategoryResponse> ProjectToResponse(IQueryable<Category> query)
    {
        return query.Select(c => new CategoryResponse
        {
            CategoryId = c.CategoryId,
            EventId = c.EventId,
            Name = c.Name,
            DistanceKm = c.DistanceKm,
            EntryFee = c.EntryFee,
            MaxParticipants = c.MaxParticipants,
            StartTime = c.StartTime,
            EnrolmentCount = c.Enrolments.Count(e => e.Status == EnrolmentStatuses.Confirmed)
        });
    }
}