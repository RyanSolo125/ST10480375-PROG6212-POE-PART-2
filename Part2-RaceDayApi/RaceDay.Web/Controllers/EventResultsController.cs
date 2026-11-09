using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Organiser: capture and publish finish times and positions after an event.</summary>
public class EventResultsController : OrganiserBaseController
{
    public EventResultsController(RaceDayApiClient api) : base(api) { }

    public async Task<IActionResult> Index(int eventId)
    {
        var eventResult = await GetMyEventAsync(eventId);
        if (!eventResult.Success) return ApiFailure(eventResult, RedirectToAction("Index", "ManageEvents"));

        var enrolments = await Api.GetEventEnrolmentsAsync(eventId);
        if (!enrolments.Success) return ApiFailure(enrolments, RedirectToAction("Index", "ManageEvents"));

        return View(new EventResultsViewModel
        {
            Event = eventResult.Data!,
            // Cancelled entries don't get results; people with a position are listed first
            Enrolments = enrolments.Data!
                .Where(en => en.Status != "Cancelled")
                .OrderBy(en => en.Position ?? int.MaxValue)
                .ThenBy(en => en.ParticipantName)
                .ToList()
        });
    }

    /// <summary>Saves one participant's result: creates it the first time, updates it after that.</summary>
    [HttpPost]
    public async Task<IActionResult> Save(ResultEntryViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ShowError(FirstModelError());
            return RedirectToAction(nameof(Index), new { eventId = model.EventId });
        }

        string? finishTime = string.IsNullOrWhiteSpace(model.FinishTime) ? null : model.FinishTime.Trim();

        var result = model.ResultId == null
            ? await Api.CaptureResultAsync(model.EnrolmentId, finishTime, model.Position, model.Status)
            : await Api.UpdateResultAsync(model.ResultId.Value, finishTime, model.Position, model.Status);

        if (!result.Success) return ApiFailure(result, RedirectToAction(nameof(Index), new { eventId = model.EventId }));

        ShowSuccess($"Result saved for {result.Data!.ParticipantName}.");
        return RedirectToAction(nameof(Index), new { eventId = model.EventId });
    }
}
