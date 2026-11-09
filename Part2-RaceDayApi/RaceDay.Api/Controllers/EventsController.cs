using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
[Produces("application/json")]
public class EventsController : BaseApiController
{
    private readonly RaceDayDbContext _db;
    private readonly IFileStorageService _storage;

    public EventsController(RaceDayDbContext db, IFileStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    // Loads events together with their organiser, categories and enrolments (for counts)
    private IQueryable<Event> EventsWithDetails()
    {
        return _db.Events
            .Include(e => e.Organiser)
            .Include(e => e.Categories)
                .ThenInclude(c => c.Enrolments);
    }

    /// <summary>Lists all events so participants can browse races (public).</summary>
    /// <param name="type">Optional filter: Run, Walk or Cycle.</param>
    /// <param name="search">Optional text to search in the event name or location.</param>
    /// <param name="upcoming">Optional: true = only events today or later.</param>
    /// <response code="200">Array of event objects.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<EventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] string? type, [FromQuery] string? search, [FromQuery] bool? upcoming)
    {
        var query = EventsWithDetails();

        string? eventType = EventTypes.Normalise(type);
        if (eventType != null)
        {
            query = query.Where(e => e.EventType == eventType);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            string text = search.Trim().ToLower();
            query = query.Where(e => e.EventName.ToLower().Contains(text) || e.Location.ToLower().Contains(text));
        }

        if (upcoming == true)
        {
            query = query.Where(e => e.EventDate >= DateTime.Today);
        }

        var events = await query.OrderBy(e => e.EventDate).ToListAsync();
        return Ok(events.Select(EventDto.FromModel).ToList());
    }

    /// <summary>Lists the logged-in organiser's own events with enrolment counts (Organisers only).</summary>
    /// <remarks>Used for the organiser dashboard.</remarks>
    /// <response code="200">Array of the organiser's events.</response>
    /// <response code="401">You are not logged in.</response>
    /// <response code="403">You are not an Organiser.</response>
    [HttpGet("mine")]
    [ProducesResponseType(typeof(List<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        var check = RequireRole(Roles.Organiser);
        if (check != null) return check;

        var events = await EventsWithDetails()
            .Where(e => e.OrganiserId == CurrentUserId)
            .OrderBy(e => e.EventDate)
            .ToListAsync();

        return Ok(events.Select(EventDto.FromModel).ToList());
    }

    /// <summary>Returns full details of one event, including its categories (public).</summary>
    /// <response code="200">Event object with nested categories.</response>
    /// <response code="404">No event with that ID.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var ev = await EventsWithDetails().FirstOrDefaultAsync(e => e.EventId == id);
        if (ev == null) return NotFound(new { message = "Event not found." });

        return Ok(EventDto.FromModel(ev));
    }

    /// <summary>Creates a new event (Organisers only).</summary>
    /// <remarks>EventType must be Run, Walk or Cycle. The logged-in organiser becomes the owner of the event.</remarks>
    /// <response code="201">New event object.</response>
    /// <response code="400">Invalid data or event type.</response>
    /// <response code="401">You are not logged in.</response>
    /// <response code="403">You are not an Organiser.</response>
    [HttpPost]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(EventCreateDto dto)
    {
        var check = RequireRole(Roles.Organiser);
        if (check != null) return check;

        string? eventType = EventTypes.Normalise(dto.EventType);
        if (eventType == null)
        {
            return BadRequest(new { message = "EventType must be Run, Walk or Cycle." });
        }

        var ev = new Event
        {
            OrganiserId = CurrentUserId!.Value,
            EventName = dto.EventName.Trim(),
            Description = dto.Description,
            EventDate = dto.EventDate.Date,
            Location = dto.Location.Trim(),
            DistanceKm = dto.DistanceKm,
            EventType = eventType
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = ev.EventId }, EventDto.FromModel(ev));
    }

    /// <summary>Updates an event the Organiser owns.</summary>
    /// <response code="200">Updated event object.</response>
    /// <response code="400">Invalid data or event type.</response>
    /// <response code="401">You are not logged in.</response>
    /// <response code="403">You are not an Organiser, or this is not your event.</response>
    /// <response code="404">No event with that ID.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, EventCreateDto dto)
    {
        var check = RequireRole(Roles.Organiser);
        if (check != null) return check;

        var ev = await EventsWithDetails().FirstOrDefaultAsync(e => e.EventId == id);
        if (ev == null) return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != CurrentUserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only update your own events." });
        }

        string? eventType = EventTypes.Normalise(dto.EventType);
        if (eventType == null)
        {
            return BadRequest(new { message = "EventType must be Run, Walk or Cycle." });
        }

        ev.EventName = dto.EventName.Trim();
        ev.Description = dto.Description;
        ev.EventDate = dto.EventDate.Date;
        ev.Location = dto.Location.Trim();
        ev.DistanceKm = dto.DistanceKm;
        ev.EventType = eventType;

        await _db.SaveChangesAsync();
        return Ok(EventDto.FromModel(ev));
    }

    /// <summary>Uploads or replaces the banner image of an event the Organiser owns.</summary>
    /// <remarks>
    /// Send the image as multipart/form-data in a field called "file" (JPG, PNG, WEBP or GIF, max 5 MB).
    /// The image is stored in Azure Blob Storage and can be loaded from GET /api/files/{bannerFileName}.
    /// </remarks>
    /// <response code="200">Updated event object with the new bannerFileName.</response>
    /// <response code="400">No file, or the file is not an allowed image.</response>
    /// <response code="401">You are not logged in.</response>
    /// <response code="403">You are not an Organiser, or this is not your event.</response>
    /// <response code="404">No event with that ID.</response>
    /// <response code="503">Azure Blob Storage is not configured.</response>
    [HttpPost("{id:int}/banner")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UploadBanner(int id, IFormFile file)
    {
        var check = RequireRole(Roles.Organiser);
        if (check != null) return check;

        var ev = await EventsWithDetails().FirstOrDefaultAsync(e => e.EventId == id);
        if (ev == null) return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != CurrentUserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only change the banner of your own events." });
        }

        string? error = ImageRules.Validate(file);
        if (error != null) return BadRequest(new { message = error });

        if (!_storage.IsConfigured)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Image storage is not available right now." });
        }

        string? oldBanner = ev.BannerFileName;

        using (var stream = file.OpenReadStream())
        {
            ev.BannerFileName = await _storage.UploadAsync(stream, ImageRules.ContentTypeOf(file), ImageRules.ExtensionOf(file));
        }
        await _db.SaveChangesAsync();

        // Remove the old banner so unused images don't pile up
        if (oldBanner != null)
        {
            await _storage.DeleteAsync(oldBanner);
        }

        return Ok(EventDto.FromModel(ev));
    }

    /// <summary>Deletes an event the Organiser owns.</summary>
    /// <remarks>Also deletes the event's categories, enrolments, results and banner image.</remarks>
    /// <response code="204">Event deleted.</response>
    /// <response code="401">You are not logged in.</response>
    /// <response code="403">You are not an Organiser, or this is not your event.</response>
    /// <response code="404">No event with that ID.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var check = RequireRole(Roles.Organiser);
        if (check != null) return check;

        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != CurrentUserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only delete your own events." });
        }

        string? banner = ev.BannerFileName;

        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();

        if (banner != null)
        {
            await _storage.DeleteAsync(banner);
        }

        return NoContent();
    }
}
