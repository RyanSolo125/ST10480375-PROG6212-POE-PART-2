using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/results")]
[Produces("application/json")]
public class ResultsController : BaseApiController
{
    private readonly RaceDayDbContext _db;

    public ResultsController(RaceDayDbContext db)
    {
        _db = db;
    }

    // Loads results together with the participant, category and event
    private IQueryable<Result> ResultsWithDetails()
    {
        return _db.Results
            .Include(r => r.Enrolment).ThenInclude(en => en!.Participant)
            .Include(r => r.Enrolment).ThenInclude(en => en!.Category).ThenInclude(c => c!.Event);
    }

    // Turns "01:45:30" into a TimeSpan. Returns false if the format is wrong.
    private static bool TryParseTime(string? text, out TimeSpan? time)
    {
        time = null;
        if (string.IsNullOrWhiteSpace(text)) return true; // finish time is optional (e.g. DNF)

        if (TimeSpan.TryParse(text, out TimeSpan parsed))
        {
            time = parsed;
            return true;
        }
        return false;
    }

    /// <summary>Captures a finish result against an existing enrolment (Organisers only).</summary>
    /// <remarks>Only allowed for your own events, on or after the event date. FinishTime format: hh:mm:ss.</remarks>
    /// <response code="201">New result object.</response>
    /// <response code="400">Invalid time format, or the event has not happened yet.</response>
    /// <response code="401">You are not logged in.</response>
    /// <response code="403">You are not an Organiser, or this is not your event.</response>
    /// <response code="404">No enrolment with that ID.</response>
    /// <response code="409">A result already exists for this enrolment (use PUT to change it).</response>
    [HttpPost]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Capture(ResultCreateDto dto)
    {
        var check = RequireRole(Roles.Organiser);
        if (check != null) return check;

        if (!TryParseTime(dto.FinishTime, out TimeSpan? finishTime))
        {
            return BadRequest(new { message = "FinishTime must be in the format hh:mm:ss, e.g. 01:45:30." });
        }

        var enrolment = await _db.Enrolments
            .Include(en => en.Category).ThenInclude(c => c!.Event)
            .FirstOrDefaultAsync(en => en.EnrolmentId == dto.EnrolmentId);

        if (enrolment == null) return NotFound(new { message = "Enrolment not found." });

        var ev = enrolment.Category!.Event!;
        if (ev.OrganiserId != CurrentUserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only capture results for your own events." });
        }

        if (ev.EventDate > DateTime.Today)
        {
            return BadRequest(new { message = "Results can only be captured after the event has taken place." });
        }

        bool resultExists = await _db.Results.AnyAsync(r => r.EnrolmentId == dto.EnrolmentId);
        if (resultExists)
        {
            return Conflict(new { message = "A result already exists for this enrolment. Use PUT to update it." });
        }

        var result = new Result
        {
            EnrolmentId = dto.EnrolmentId,
            FinishTime = finishTime,
            Position = dto.Position,
            Status = dto.Status
        };

        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        var saved = await ResultsWithDetails().FirstAsync(r => r.ResultId == result.ResultId);
        return StatusCode(StatusCodes.Status201Created, ResultDto.FromModel(saved));
    }

    /// <summary>Updates a previously captured result, e.g. correcting a time (Organisers only).</summary>
    /// <response code="200">Updated result object.</response>
    /// <response code="400">Invalid time format.</response>
    /// <response code="401">You are not logged in.</response>
    /// <response code="403">You are not an Organiser, or this is not your event.</response>
    /// <response code="404">No result with that ID.</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, ResultUpdateDto dto)
    {
        var check = RequireRole(Roles.Organiser);
        if (check != null) return check;

        if (!TryParseTime(dto.FinishTime, out TimeSpan? finishTime))
        {
            return BadRequest(new { message = "FinishTime must be in the format hh:mm:ss, e.g. 01:45:30." });
        }

        var result = await ResultsWithDetails().FirstOrDefaultAsync(r => r.ResultId == id);
        if (result == null) return NotFound(new { message = "Result not found." });

        if (result.Enrolment!.Category!.Event!.OrganiserId != CurrentUserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only update results for your own events." });
        }

        result.FinishTime = finishTime;
        result.Position = dto.Position;
        result.Status = dto.Status;
        await _db.SaveChangesAsync();

        return Ok(ResultDto.FromModel(result));
    }

    /// <summary>Returns the logged-in participant's own result history (Participants only).</summary>
    /// <response code="200">Array of result objects.</response>
    /// <response code="401">You are not logged in.</response>
    /// <response code="403">You are not a Participant.</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(List<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyResults()
    {
        var check = RequireRole(Roles.Participant);
        if (check != null) return check;

        var results = await ResultsWithDetails()
            .Where(r => r.Enrolment!.ParticipantId == CurrentUserId)
            .ToListAsync();

        return Ok(results.Select(ResultDto.FromModel).ToList());
    }

    /// <summary>Returns the full results/leaderboard for an event, ordered by position (public).</summary>
    /// <response code="200">Array of result objects.</response>
    /// <response code="404">No event with that ID.</response>
    [HttpGet("/api/events/{eventId}/results")]
    [ProducesResponseType(typeof(List<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        bool eventExists = await _db.Events.AnyAsync(e => e.EventId == eventId);
        if (!eventExists) return NotFound(new { message = "Event not found." });

        var results = await ResultsWithDetails()
            .Where(r => r.Enrolment!.Category!.EventId == eventId)
            .OrderBy(r => r.Position)
            .ToListAsync();

        return Ok(results.Select(ResultDto.FromModel).ToList());
    }
}
