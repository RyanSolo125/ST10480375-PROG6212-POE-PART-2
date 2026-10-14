using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : BaseApiController
{
    private readonly RaceDayDbContext _db;

    public AuthController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>Registers a new user as either an Organiser or a Participant.</summary>
    /// <remarks>
    /// Send your personal details and choose a role ("Organiser" or "Participant").
    /// The password is hashed with BCrypt and is never stored in its original form.
    /// </remarks>
    /// <response code="201">Account created. Returns the new user (no password returned).</response>
    /// <response code="400">Validation error, e.g. missing fields or an invalid role.</response>
    /// <response code="409">The email address is already registered.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        string? role = Roles.Normalise(dto.Role);
        if (role == null)
        {
            return BadRequest(new { message = "Role must be either 'Organiser' or 'Participant'." });
        }

        string email = dto.Email.Trim().ToLower();

        bool emailTaken = await _db.Users.AnyAsync(u => u.Email == email);
        if (emailTaken)
        {
            return Conflict(new { message = "An account with this email already exists." });
        }

        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = role,
            PhoneNumber = dto.PhoneNumber
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, UserDto.FromModel(user));
    }

    /// <summary>Authenticates a user and starts a session.</summary>
    /// <remarks>
    /// Stores your UserId and Role in the server session.
    /// The returned token is the session ID. The browser also gets it as a cookie
    /// and sends it automatically with every request after this.
    /// </remarks>
    /// <response code="200">Login successful. Returns { token, userId, fullName, role }.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        string email = dto.Email.Trim().ToLower();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        // Save the logged-in user in the session
        HttpContext.Session.SetInt32(SessionKeys.UserId, user.UserId);
        HttpContext.Session.SetString(SessionKeys.Role, user.Role);
        HttpContext.Session.SetString(SessionKeys.FullName, user.FullName);

        return Ok(new LoginResponseDto
        {
            Token = HttpContext.Session.Id,
            UserId = user.UserId,
            FullName = user.FullName,
            Role = user.Role
        });
    }

    /// <summary>Logs out and ends the current session.</summary>
    /// <response code="200">Logged out.</response>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new { message = "Logged out successfully." });
    }
}
