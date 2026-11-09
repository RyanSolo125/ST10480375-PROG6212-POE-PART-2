using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Web.Helpers;

/// <summary>
/// Put [RequireLogin] on a controller or action to block users who are not logged in.
/// Use [RequireLogin(Role = Roles.Organiser)] to also require a specific role.
/// </summary>
public class RequireLoginAttribute : ActionFilterAttribute
{
    public string? Role { get; set; }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;

        // Not logged in -> go to the login page, then come back here afterwards
        if (session.GetInt32(SessionKeys.UserId) == null)
        {
            string returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }

        // Logged in, but the wrong role -> access denied page
        if (Role != null && session.GetString(SessionKeys.Role) != Role)
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Home", null);
        }
    }
}
