using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Both roles: view and update your profile and profile picture.</summary>
[RequireLogin]
public class ProfileController : BaseController
{
    public ProfileController(RaceDayApiClient api) : base(api) { }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await Api.GetProfileAsync();
        if (!result.Success) return ApiFailure(result, RedirectToAction("Index", "Home"));

        var user = result.Data!;
        return View(new ProfileViewModel
        {
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            Email = user.Email,
            Role = user.Role,
            CreatedAt = user.CreatedAt,
            ProfilePictureFileName = user.ProfilePictureFileName
        });
    }

    [HttpPost]
    public async Task<IActionResult> Index(ProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await FillReadOnlyDetails(model);
            return View(model);
        }

        var result = await Api.UpdateProfileAsync(model.FullName, model.PhoneNumber);
        if (!result.Success) return ApiFailure(result, RedirectToAction(nameof(Index)));

        HttpContext.Session.SetString(SessionKeys.FullName, result.Data!.FullName);
        ShowSuccess("Your profile has been updated.");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Sends the picture to the API, which stores it in Azure Blob Storage.</summary>
    [HttpPost]
    public async Task<IActionResult> UploadPicture(IFormFile? picture)
    {
        if (picture == null || picture.Length == 0)
        {
            ShowError("Please choose an image to upload.");
            return RedirectToAction(nameof(Index));
        }

        var result = await Api.UploadProfilePictureAsync(picture);
        if (!result.Success) return ApiFailure(result, RedirectToAction(nameof(Index)));

        if (result.Data!.ProfilePictureFileName != null)
        {
            HttpContext.Session.SetString(SessionKeys.ProfilePicture, result.Data.ProfilePictureFileName);
        }

        ShowSuccess("Your profile picture has been updated.");
        return RedirectToAction(nameof(Index));
    }

    // When the form has errors we still need the email, role etc. to redraw the page
    private async Task FillReadOnlyDetails(ProfileViewModel model)
    {
        var result = await Api.GetProfileAsync();
        if (result.Success)
        {
            model.Email = result.Data!.Email;
            model.Role = result.Data.Role;
            model.CreatedAt = result.Data.CreatedAt;
            model.ProfilePictureFileName = result.Data.ProfilePictureFileName;
        }
    }
}
