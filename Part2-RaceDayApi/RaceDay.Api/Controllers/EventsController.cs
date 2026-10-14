using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
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

    /// <summary>Lists all events so participants can browse upcoming races (public).</summary>
    /// <param name="type">Optional filter: Run, Walk or Cycle.</param>
    /// <response code="200">Array of event objects.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<EventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] string? type)
    {
        var query = _db.Events
            .Include(e => e.Organiser)
            .Include(e => e.Categories)
            .AsQueryable();

        string? eventType = EventTypes.Normalise(type);
        if (eventType != null)
        {
            query = query.Where(e => e.EventType == eventType);
        }

        var events = await query.OrderBy(e => e.EventDate).ToListAsync();
        return Ok(events.Select(EventDto.FromModel).ToList());
    }

    /// <summary>Returns full details of one event, including its categories (public).</summary>
    /// <response code="200">Event object with nested categories.</response>
    /// <response code="404">No event with that ID.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var ev = await _db.Events
            .Include(e => e.Organiser)
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventId == id);

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
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, EventCreateDto dto)
    {
        var check = RequireRole(Roles.Organiser);
        if (check != null) return check;

        var ev = await _db.Events.Include(e => e.Categories).FirstOrDefaultAsync(e => e.EventId == id);
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

    /// <summary>Deletes an event the Organiser owns.</summary>
    /// <remarks>Also deletes the event's categories, enrolments and results.</remarks>
    /// <response code="204">Event deleted.</response>
    /// <response code="401">You are not logged in.</response>
    /// <response code="403">You are not an Organiser, or this is not your event.</response>
    /// <response code="404">No event with that ID.</response>
    [HttpDelete("{id}")]
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

        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
