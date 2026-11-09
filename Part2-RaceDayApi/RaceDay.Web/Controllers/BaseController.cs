using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Helpers;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>
/// Shared code for all MVC controllers: who is logged in, success/error messages,
/// and what to do when an API call fails.
/// </summary>
public abstract class BaseController : Controller
{
    protected readonly RaceDayApiClient Api;

    protected BaseController(RaceDayApiClient api)
    {
        Api = api;
    }

    protected int? CurrentUserId => HttpContext.Session.GetInt32(SessionKeys.UserId);
    protected string? CurrentRole => HttpContext.Session.GetString(SessionKeys.Role);
    protected bool IsLoggedIn => CurrentUserId != null;

    /// <summary>Green message shown at the top of the next page.</summary>
    protected void ShowSuccess(string message) => TempData["Success"] = message;

    /// <summary>Red message shown at the top of the next page.</summary>
    protected void ShowError(string message) => TempData["Error"] = message;

    /// <summary>
    /// Handles a failed API call:
    /// 401 = the API session expired -> log out and go to the login page,
    /// 403 = not allowed -> access denied page,
    /// anything else -> show the API's message and go to the fallback page.
    /// </summary>
    protected IActionResult ApiFailure(ApiResult result, IActionResult fallback)
    {
        if (result.StatusCode == 401)
        {
            HttpContext.Session.Clear();
            ShowError("Your session has expired. Please log in again.");
            return RedirectToAction("Login", "Account");
        }

        if (result.StatusCode == 403)
        {
            return RedirectToAction("AccessDenied", "Home");
        }

        if (result.StatusCode == 404)
        {
            return RedirectToAction("HttpStatus", "Home", new { code = 404 });
        }

        ShowError(result.ErrorMessage ?? "Something went wrong. Please try again.");
        return fallback;
    }
}
