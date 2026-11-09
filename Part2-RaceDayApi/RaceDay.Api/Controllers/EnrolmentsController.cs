using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/enrolments")]
[Produces("application/json")]
public class EnrolmentsController : BaseApiController
{
    private readonly RaceDayDbContext _db;

    public EnrolmentsController(RaceDayDbContext db)
    {
        _db = db;
    }

    // Loads enrolments together with the participant, category and event
    private IQueryable<Enrolment> EnrolmentsWithDetails()
    {
        return _db.Enrolments
            .Include(en => en.Participant)
            .Include(en => en.Category)
                .ThenInclude(c => c!.Event)
            .Include(en => en.Result);
    }

    /// <summary>Enrols the logged-in participant into a category of an event (Participants only).</summary>
    /// <remarks>
    /// Records the link between the participant, the event and the selected category straight away.
    /// New enrolments start as "Pending" until the organiser confirms them.
    /// </remarks>
    /// <response code="201">New enrolment object.</response>
    /// <response code="400">Category full, or the event has already taken place.</response>
    /// <response code="401">You are not logged in.</response>
    /// <response code="403">You are not a Participant.</response>
    /// <response code="404">No category with that ID.</response>
    /// <response code="409">Already enrolled in this event.</response>
    [HttpPost]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(EnrolmentCreateDto dto)
    {
        var check = RequireRole(Roles.Participant);
        if (check != null) return check;

        int participantId = CurrentUserId!.Value;

        var category = await _db.Categories
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryId == dto.CategoryId);

        if (category == null || category.Event == null)
        {
            return NotFound(new { message = "Category not found." });
        }

        if (category.Event.EventDate < DateTime.Today)
        {
            return BadRequest(new { message = "You cannot enter an event that has already taken place." });
        }

        bool alreadyEnrolled = await _db.Enrolments.AnyAsync(en =>
            en.ParticipantId == participantId && en.Category!.EventId == category.EventId);
        if (alreadyEnrolled)
        {
            return Conflict(new { message = "You are already enrolled in this event." });
        }

        if (category.MaxParticipants != null)
        {
            int count = await _db.Enrolments.CountAsync(en => en.CategoryId == category.CategoryId);
            if (count >= category.MaxParticipants)
            {
                return BadRequest(new { message = "This category is full." });
            }
        }

        var enrolment = new Enrolment
        {
            ParticipantId = participantId,
            CategoryId = category.CategoryId,
            Status = EnrolmentStatuses.Pending
        };

        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        var saved = await EnrolmentsWithDetails().FirstAsync(en => en.EnrolmentId == enrolment.EnrolmentId);
        return StatusCode(StatusCodes.Status201Created, EnrolmentDto.FromModel(saved));
    }

    /// <summary>Lists all enrolments belonging to the logged-in participant (Participants only).</summary>
    /// <response code="200">Array of enrolment objects.</response>
    /// <response code="401">You are not logged in.</response>
    /// <response code="403">You are not a Participant.</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(List<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyEnrolments()
    {
        var check = RequireRole(Roles.Participant);
        if (check != null) return check;

        var enrolments = await EnrolmentsWithDetails()
            .Where(en => en.ParticipantId == CurrentUserId)
            .ToListAsync();

        return Ok(enrolments.Select(EnrolmentDto.FromModel).ToList());
    }

    /// <summary>Lists every enrolment for an event, so the Organiser who owns it can manage participants.</summary>
    /// <response code="200">Array of enrolments with participant details.</response>
    /// <response code="401">You are not logged in.</response>
    /// <response code="403">You are not an Organiser, or this is not your event.</response>
    /// <response code="404">No event with that ID.</response>
    [HttpGet("/api/events/{eventId}/enrolments")]
    [ProducesResponseType(typeof(List<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var check = RequireRole(Roles.Organiser);
        if (check != null) return check;

        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != CurrentUserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only view enrolments for your own events." });
        }

        var enrolments = await EnrolmentsWithDetails()
            .Where(en => en.Category!.EventId == eventId)
            .ToListAsync();

        return Ok(enrolments.Select(EnrolmentDto.FromModel).ToList());
    }

    /// <summary>Changes the status of an enrolment on one of your events (Organisers only).</summary>
    /// <remarks>Status must be Pending, Confirmed or Cancelled.</remarks>
    /// <response code="200">Updated enrolment object.</response>
    /// <response code="400">Invalid status.</response>
    /// <response code="401">You are not logged in.</response>
    /// <response code="403">You are not an Organiser, or this is not your event.</response>
    /// <response code="404">No enrolment with that ID.</response>
    [HttpPut("{id}/status")]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(int id, EnrolmentStatusDto dto)
    {
        var check = RequireRole(Roles.Organiser);
        if (check != null) return check;

        string? status = EnrolmentStatuses.Normalise(dto.Status);
        if (status == null)
        {
            return BadRequest(new { message = "Status must be Pending, Confirmed or Cancelled." });
        }

        var enrolment = await EnrolmentsWithDetails().FirstOrDefaultAsync(en => en.EnrolmentId == id);
        if (enrolment == null) return NotFound(new { message = "Enrolment not found." });

        if (enrolment.Category!.Event!.OrganiserId != CurrentUserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only manage enrolments for your own events." });
        }

        enrolment.Status = status;
        await _db.SaveChangesAsync();

        return Ok(EnrolmentDto.FromModel(enrolment));
    }

    /// <summary>Cancels the logged-in participant's own enrolment (Participants only).</summary>
    /// <response code="204">Enrolment cancelled.</response>
    /// <response code="401">You are not logged in.</response>
    /// <response code="403">You are not a Participant, or this is not your enrolment.</response>
    /// <response code="404">No enrolment with that ID.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(int id)
    {
        var check = RequireRole(Roles.Participant);
        if (check != null) return check;

        var enrolment = await _db.Enrolments.FindAsync(id);
        if (enrolment == null) return NotFound(new { message = "Enrolment not found." });

        if (enrolment.ParticipantId != CurrentUserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only cancel your own enrolments." });
        }

        _db.Enrolments.Remove(enrolment);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
