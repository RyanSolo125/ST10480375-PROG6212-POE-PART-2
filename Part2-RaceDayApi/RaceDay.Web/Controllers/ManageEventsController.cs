using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Organiser: create, edit and delete events (with an optional banner image).</summary>
public class ManageEventsController : OrganiserBaseController
{
    public ManageEventsController(RaceDayApiClient api) : base(api) { }

    /// <summary>List of the organiser's events with links to manage each one.</summary>
    public async Task<IActionResult> Index()
    {
        var result = await Api.GetMyEventsAsync();
        if (!result.Success) return ApiFailure(result, RedirectToAction("Index", "Dashboard"));

        return View(result.Data!.OrderByDescending(e => e.IsUpcoming).ThenBy(e => e.EventDate).ToList());
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new EventFormViewModel { EventDate = DateTime.Today.AddMonths(1) });
    }

    [HttpPost]
    public async Task<IActionResult> Create(EventFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var created = await Api.CreateEventAsync(model.ToApiRequest());
        if (!created.Success)
        {
            if (created.StatusCode == 401 || created.StatusCode == 403) return ApiFailure(created, RedirectToAction(nameof(Index)));

            ModelState.AddModelError(string.Empty, created.ErrorMessage ?? "The event could not be created.");
            return View(model);
        }

        int eventId = created.Data!.EventId;
        await TryUploadBanner(eventId, model);

        ShowSuccess($"\"{model.EventName}\" was created. Now add its categories.");
        return RedirectToAction("Index", "Categories", new { eventId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await GetMyEventAsync(id);
        if (!result.Success) return ApiFailure(result, RedirectToAction(nameof(Index)));

        var ev = result.Data!;
        return View(new EventFormViewModel
        {
            EventId = ev.EventId,
            EventName = ev.EventName,
            Description = ev.Description,
            EventDate = ev.EventDate,
            Location = ev.Location,
            DistanceKm = ev.DistanceKm,
            EventType = ev.EventType,
            ExistingBannerFileName = ev.BannerFileName
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model)
    {
        model.EventId = id;
        if (!ModelState.IsValid) return View(model);

        var updated = await Api.UpdateEventAsync(id, model.ToApiRequest());
        if (!updated.Success)
        {
            if (updated.StatusCode == 401 || updated.StatusCode == 403 || updated.StatusCode == 404)
            {
                return ApiFailure(updated, RedirectToAction(nameof(Index)));
            }

            ModelState.AddModelError(string.Empty, updated.ErrorMessage ?? "The event could not be saved.");
            return View(model);
        }

        await TryUploadBanner(id, model);

        ShowSuccess($"\"{model.EventName}\" was updated.");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Confirmation page before deleting.</summary>
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await GetMyEventAsync(id);
        if (!result.Success) return ApiFailure(result, RedirectToAction(nameof(Index)));

        return View(result.Data);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await Api.DeleteEventAsync(id);
        if (!result.Success) return ApiFailure(result, RedirectToAction(nameof(Index)));

        ShowSuccess("The event was deleted.");
        return RedirectToAction(nameof(Index));
    }

    // Sends the banner (if one was chosen) to the API, which stores it in Azure Blob Storage.
    // The event is already saved, so a failed upload is a warning, not an error.
    private async Task TryUploadBanner(int eventId, EventFormViewModel model)
    {
        if (model.BannerImage == null || model.BannerImage.Length == 0) return;

        var upload = await Api.UploadBannerAsync(eventId, model.BannerImage);
        if (!upload.Success)
        {
            TempData["Warning"] = "The event was saved, but the banner image was not uploaded: " + upload.ErrorMessage;
        }
    }
}
