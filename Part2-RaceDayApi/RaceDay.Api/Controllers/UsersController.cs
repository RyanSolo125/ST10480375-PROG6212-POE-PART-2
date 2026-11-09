using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Helpers;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/users/me")]
[Produces("application/json")]
public class UsersController : BaseApiController
{
    private readonly RaceDayDbContext _db;
    private readonly IFileStorageService _storage;

    public UsersController(RaceDayDbContext db, IFileStorageService storage)
    {
        _db = db;
        _storage = storage;
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

        var finisherCounts = await ResultStats.FinisherCountsAsync(_db, enrolments.Select(en => en.Category!.EventId));

        var history = enrolments
            .Select(en => HistoryDto.FromModel(en, finisherCounts.GetValueOrDefault(en.Category!.EventId)))
            .ToList();

        return Ok(history);
    }

    /// <summary>Uploads or replaces the logged-in user's profile picture (any role).</summary>
    /// <remarks>
    /// Send the image as multipart/form-data in a field called "file" (JPG, PNG, WEBP or GIF, max 5 MB).
    /// The image is stored in Azure Blob Storage and can be loaded from GET /api/files/{profilePictureFileName}.
    /// </remarks>
    /// <response code="200">Updated profile with the new profilePictureFileName.</response>
    /// <response code="400">No file, or the file is not an allowed image.</response>
    /// <response code="401">You are not logged in.</response>
    /// <response code="503">Azure Blob Storage is not configured.</response>
    [HttpPost("profile-picture")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file)
    {
        var check = RequireLogin();
        if (check != null) return check;

        string? error = ImageRules.Validate(file);
        if (error != null) return BadRequest(new { message = error });

        if (!_storage.IsConfigured)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Image storage is not available right now." });
        }

        var user = await _db.Users.FindAsync(CurrentUserId!.Value);
        if (user == null) return NotFound(new { message = "User not found." });

        string? oldPicture = user.ProfilePictureFileName;

        using (var stream = file.OpenReadStream())
        {
            user.ProfilePictureFileName = await _storage.UploadAsync(stream, ImageRules.ContentTypeOf(file), ImageRules.ExtensionOf(file));
        }
        await _db.SaveChangesAsync();

        if (oldPicture != null)
        {
            await _storage.DeleteAsync(oldPicture);
        }

        return Ok(UserDto.FromModel(user));
    }
}
