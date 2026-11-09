using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Organiser: see everyone enrolled in an event and confirm or cancel their enrolment.</summary>
public class EventEnrolmentsController : OrganiserBaseController
{
    public EventEnrolmentsController(RaceDayApiClient api) : base(api) { }

    public async Task<IActionResult> Index(int eventId)
    {
        var eventResult = await GetMyEventAsync(eventId);
        if (!eventResult.Success) return ApiFailure(eventResult, RedirectToAction("Index", "ManageEvents"));

        var enrolments = await Api.GetEventEnrolmentsAsync(eventId);
        if (!enrolments.Success) return ApiFailure(enrolments, RedirectToAction("Index", "ManageEvents"));

        return View(new EventEnrolmentsViewModel
        {
            Event = eventResult.Data!,
            Enrolments = enrolments.Data!.OrderBy(en => en.CategoryName).ThenBy(en => en.ParticipantName).ToList()
        });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int eventId, int enrolmentId, string status)
    {
        var result = await Api.UpdateEnrolmentStatusAsync(enrolmentId, status);
        if (!result.Success) return ApiFailure(result, RedirectToAction(nameof(Index), new { eventId }));

        ShowSuccess($"{result.Data!.ParticipantName}'s enrolment is now {result.Data.Status}.");
        return RedirectToAction(nameof(Index), new { eventId });
    }
}
