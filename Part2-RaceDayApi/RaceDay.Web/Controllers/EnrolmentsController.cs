using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Helpers;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Participant: enter events and see "My Enrolments".</summary>
[RequireLogin(Role = Roles.Participant)]
public class EnrolmentsController : BaseController
{
    public EnrolmentsController(RaceDayApiClient api) : base(api) { }

    /// <summary>All events the participant has entered, with their status.</summary>
    public async Task<IActionResult> Index()
    {
        var result = await Api.GetMyEnrolmentsAsync();
        if (!result.Success) return ApiFailure(result, RedirectToAction("Index", "Home"));

        var enrolments = result.Data!.OrderByDescending(en => en.EventDate >= DateTime.Today)
                                     .ThenBy(en => en.EventDate)
                                     .ToList();
        return View(enrolments);
    }

    /// <summary>Enrols the participant in the chosen category (recorded straight away by the API).</summary>
    [HttpPost]
    public async Task<IActionResult> Create(int eventId, int? categoryId)
    {
        if (categoryId == null)
        {
            ShowError("Please choose a category to enter.");
            return RedirectToAction("Details", "Events", new { id = eventId });
        }

        var result = await Api.EnrolAsync(categoryId.Value);
        if (!result.Success)
        {
            return ApiFailure(result, RedirectToAction("Details", "Events", new { id = eventId }));
        }

        ShowSuccess($"You're entered in {result.Data!.EventName} ({result.Data.CategoryName})! Your status is {result.Data.Status} until the organiser confirms it.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await Api.CancelEnrolmentAsync(id);
        if (!result.Success) return ApiFailure(result, RedirectToAction(nameof(Index)));

        ShowSuccess("Your enrolment has been cancelled.");
        return RedirectToAction(nameof(Index));
    }
}
