using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
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

    /// <summary>Lists all categories that belong to a specific event (public).</summary>
    /// <response code="200">Array of category objects.</response>
    /// <response code="404">No event with that ID.</response>
    [HttpGet("events/{eventId}/categories")]
    [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        bool eventExists = await _db.Events.AnyAsync(e => e.EventId == eventId);
        if (!eventExists) return NotFound(new { message = "Event not found." });

        var categories = await _db.Categories
            .Where(c => c.EventId == eventId)
            .ToListAsync();

        return Ok(categories.Select(CategoryDto.FromModel).ToList());
    }

    /// <summary>Adds a new age or distance category (e.g. "Under 20", "10km Run") to an event the Organiser owns.</summary>
    /// <response code="201">New category object.</response>
    /// <response code="400">Invalid data.</response>
    /// <response code="401">You are not logged in.</response>
    /// <response code="403">You are not an Organiser, or this is not your event.</response>
    /// <response code="404">No event with that ID.</response>
    [HttpPost("events/{eventId}/categories")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(int eventId, CategoryCreateDto dto)
    {
        var check = RequireRole(Roles.Organiser);
        if (check != null) return check;

        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != CurrentUserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only add categories to your own events." });
        }

        var category = new Category
        {
            EventId = eventId,
            CategoryName = dto.CategoryName.Trim(),
            DistanceKm = dto.DistanceKm,
            EntryFee = dto.EntryFee,
            MaxParticipants = dto.MaxParticipants
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, CategoryDto.FromModel(category));
    }

    /// <summary>Updates an existing category on an event the Organiser owns.</summary>
    /// <response code="200">Updated category object.</response>
    /// <response code="401">You are not logged in.</response>
    /// <response code="403">You are not an Organiser, or this is not your event.</response>
    /// <response code="404">No category with that ID.</response>
    [HttpPut("categories/{id}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, CategoryCreateDto dto)
    {
        var check = RequireRole(Roles.Organiser);
        if (check != null) return check;

        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category == null) return NotFound(new { message = "Category not found." });

        if (category.Event!.OrganiserId != CurrentUserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only update categories on your own events." });
        }

        category.CategoryName = dto.CategoryName.Trim();
        category.DistanceKm = dto.DistanceKm;
        category.EntryFee = dto.EntryFee;
        category.MaxParticipants = dto.MaxParticipants;

        await _db.SaveChangesAsync();
        return Ok(CategoryDto.FromModel(category));
    }

    /// <summary>Removes a category from an event the Organiser owns.</summary>
    /// <response code="204">Category deleted.</response>
    /// <response code="401">You are not logged in.</response>
    /// <response code="403">You are not an Organiser, or this is not your event.</response>
    /// <response code="404">No category with that ID.</response>
    [HttpDelete("categories/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var check = RequireRole(Roles.Organiser);
        if (check != null) return check;

        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category == null) return NotFound(new { message = "Category not found." });

        if (category.Event!.OrganiserId != CurrentUserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only delete categories on your own events." });
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
