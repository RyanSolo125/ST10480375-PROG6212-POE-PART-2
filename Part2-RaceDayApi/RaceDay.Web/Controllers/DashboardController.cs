using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Organiser dashboard: summary of their events, enrolments per event and upcoming dates.</summary>
public class DashboardController : OrganiserBaseController
{
    public DashboardController(RaceDayApiClient api) : base(api) { }

    public async Task<IActionResult> Index()
    {
        var model = new DashboardViewModel
        {
            OrganiserName = HttpContext.Session.GetString(SessionKeys.FullName) ?? "Organiser"
        };

        var result = await Api.GetMyEventsAsync();
        if (!result.Success)
        {
            if (result.StatusCode == 401 || result.StatusCode == 403)
            {
                return ApiFailure(result, RedirectToAction("Index", "Home"));
            }

            ViewBag.LoadError = result.ErrorMessage;
            return View(model);
        }

        model.Events = result.Data!;
        return View(model);
    }
}
