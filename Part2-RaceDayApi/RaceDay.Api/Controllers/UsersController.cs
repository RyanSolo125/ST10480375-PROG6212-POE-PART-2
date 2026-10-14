using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Helpers;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/users/me")]
[Produces("application/json")]
public class UsersController : BaseApiController
{
    private readonly RaceDayDbContext _db;

    public UsersController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>Returns the profile of the currently logged-in user (any role).</summary>
    /// <response code="200">Your profile.</response>
    /// <response code="401">You are not logged in.</response>
    [HttpGet]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile()
    {
        var check = RequireLogin();
        if (check != null) return check;

        var user = await _db.Users.FindAsync(CurrentUserId!.Value);
        if (user == null) return NotFound(new { message = "User not found." });

        return Ok(UserDto.FromModel(user));
    }

    /// <summary>Updates the logged-in user's own profile details (any role).</summary>
    /// <response code="200">Updated profile.</response>
    /// <response code="400">Invalid data.</response>
    /// <response code="401">You are not logged in.</response>
    [HttpPut]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile(UpdateProfileDto dto)
    {
        var check = RequireLogin();
        if (check != null) return check;

        var user = await _db.Users.FindAsync(CurrentUserId!.Value);
        if (user == null) return NotFound(new { message = "User not found." });

        user.FullName = dto.FullName.Trim();
        user.PhoneNumber = dto.PhoneNumber;
        await _db.SaveChangesAsync();

        HttpContext.Session.SetString(SessionKeys.FullName, user.FullName);

        return Ok(UserDto.FromModel(user));
    }

    /// <summary>Returns the logged-in participant's personal enrolment and result history (Participants only).</summary>
    /// <response code="200">Array of { event, category, enrolmentDate, result }.</response>
    /// <response code="401">You are not logged in.</response>
    /// <response code="403">You are not a Participant.</response>
    [HttpGet("history")]
    [ProducesResponseType(typeof(List<HistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetHistory()
    {
        var check = RequireRole(Roles.Participant);
        if (check != null) return check;

        var enrolments = await _db.Enrolments
            .Include(en => en.Category).ThenInclude(c => c!.Event)
            .Include(en => en.Result)
            .Where(en => en.ParticipantId == CurrentUserId)
            .OrderByDescending(en => en.Category!.Event!.EventDate)
            .ToListAsync();

        return Ok(enrolments.Select(HistoryDto.FromModel).ToList());
    }
}
