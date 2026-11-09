using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Organiser: add, edit and remove age or distance categories for an event.</summary>
public class CategoriesController : OrganiserBaseController
{
    public CategoriesController(RaceDayApiClient api) : base(api) { }

    public async Task<IActionResult> Index(int eventId)
    {
        var result = await GetMyEventAsync(eventId);
        if (!result.Success) return ApiFailure(result, RedirectToAction("Index", "ManageEvents"));

        var ev = result.Data!;
        return View(new CategoriesPageViewModel
        {
            Event = ev,
            Categories = ev.Categories,
            NewCategory = new CategoryFormViewModel { EventId = eventId, DistanceKm = ev.DistanceKm }
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([Bind(Prefix = "NewCategory")] CategoryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ShowError(FirstModelError());
            return RedirectToAction(nameof(Index), new { eventId = model.EventId });
        }

        var result = await Api.CreateCategoryAsync(model.EventId, model.ToApiRequest());
        if (!result.Success) return ApiFailure(result, RedirectToAction(nameof(Index), new { eventId = model.EventId }));

        ShowSuccess($"Category \"{model.CategoryName}\" was added.");
        return RedirectToAction(nameof(Index), new { eventId = model.EventId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, int eventId)
    {
        var result = await GetMyEventAsync(eventId);
        if (!result.Success) return ApiFailure(result, RedirectToAction("Index", "ManageEvents"));

        var category = result.Data!.Categories.FirstOrDefault(c => c.CategoryId == id);
        if (category == null) return RedirectToAction("HttpStatus", "Home", new { code = 404 });

        ViewBag.EventName = result.Data.EventName;
        return View(new CategoryFormViewModel
        {
            CategoryId = category.CategoryId,
            EventId = eventId,
            CategoryName = category.CategoryName,
            DistanceKm = category.DistanceKm,
            EntryFee = category.EntryFee,
            MaxParticipants = category.MaxParticipants
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(CategoryFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await Api.UpdateCategoryAsync(model.CategoryId, model.ToApiRequest());
        if (!result.Success) return ApiFailure(result, RedirectToAction(nameof(Index), new { eventId = model.EventId }));

        ShowSuccess($"Category \"{model.CategoryName}\" was updated.");
        return RedirectToAction(nameof(Index), new { eventId = model.EventId });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, int eventId)
    {
        var result = await Api.DeleteCategoryAsync(id);
        if (!result.Success) return ApiFailure(result, RedirectToAction(nameof(Index), new { eventId }));

        ShowSuccess("The category was removed.");
        return RedirectToAction(nameof(Index), new { eventId });
    }
}
