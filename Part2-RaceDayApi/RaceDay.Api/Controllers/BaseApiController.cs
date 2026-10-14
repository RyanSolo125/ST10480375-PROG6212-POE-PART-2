using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Helpers;

namespace RaceDay.Api.Controllers;

/// <summary>
/// All controllers inherit from this class so they can check
/// who is logged in (from the session) and what their role is.
/// </summary>
public abstract class BaseApiController : ControllerBase
{
    protected int? CurrentUserId => HttpContext.Session.GetInt32(SessionKeys.UserId);
    protected string? CurrentRole => HttpContext.Session.GetString(SessionKeys.Role);

    /// <summary>Returns a 401 result if no one is logged in, otherwise null.</summary>
    protected IActionResult? RequireLogin()
    {
        if (CurrentUserId == null)
        {
            return Unauthorized(new { message = "You must be logged in to access this resource." });
        }
        return null;
    }

    /// <summary>Returns 401 if not logged in, 403 if the wrong role, otherwise null.</summary>
    protected IActionResult? RequireRole(string role)
    {
        var loginCheck = RequireLogin();
        if (loginCheck != null) return loginCheck;

        if (CurrentRole != role)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = $"Only {role}s can access this resource." });
        }
        return null;
    }
}
