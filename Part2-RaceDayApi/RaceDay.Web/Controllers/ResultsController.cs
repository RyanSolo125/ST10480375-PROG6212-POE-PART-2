using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Helpers;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Participant: "My Results" - race history for completed events.</summary>
[RequireLogin(Role = Roles.Participant)]
public class ResultsController : BaseController
{
    public ResultsController(RaceDayApiClient api) : base(api) { }

    public async Task<IActionResult> Index()
    {
        var result = await Api.GetHistoryAsync();
        if (!result.Success) return ApiFailure(result, RedirectToAction("Index", "Home"));

        // Only events that have already taken place, newest first
        var completed = result.Data!
            .Where(h => h.EventDate.Date < DateTime.Today || h.ResultStatus != null)
            .OrderByDescending(h => h.EventDate)
            .ToList();

        return View(completed);
    }
}
