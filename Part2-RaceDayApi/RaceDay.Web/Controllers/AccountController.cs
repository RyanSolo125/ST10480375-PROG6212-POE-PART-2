using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController : BaseController
{
    public AccountController(RaceDayApiClient api) : base(api) { }

    [HttpGet]
    public IActionResult Login(string? returnUrl)
    {
        if (IsLoggedIn) return RedirectToHome();
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        // The API checks the password and starts the session
        var result = await Api.LoginAsync(model.Email, model.Password);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Login failed.");
            return View(model);
        }

        // Load the profile picture for the navigation bar
        var profile = await Api.GetProfileAsync();
        if (profile.Success && profile.Data?.ProfilePictureFileName != null)
        {
            HttpContext.Session.SetString(SessionKeys.ProfilePicture, profile.Data.ProfilePictureFileName);
        }

        ShowSuccess($"Welcome back, {result.Data!.FullName}!");

        // Only redirect to local pages (stops redirects to other websites)
        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToHome();
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (IsLoggedIn) return RedirectToHome();
        return View(new RegisterViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await Api.RegisterAsync(new
        {
            fullName = model.FullName,
            email = model.Email,
            password = model.Password,
            role = model.Role,
            phoneNumber = model.PhoneNumber
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Registration failed.");
            return View(model);
        }

        // Log the new user straight in
        var login = await Api.LoginAsync(model.Email, model.Password);
        if (!login.Success)
        {
            ShowSuccess("Your account was created. Please log in.");
            return RedirectToAction(nameof(Login));
        }

        ShowSuccess($"Welcome to RaceDay, {model.FullName}!");
        return RedirectToHome();
    }

    /// <summary>Ends the session in the API and in this app, then goes back to the home page.</summary>
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await Api.LogoutAsync();   // even if the API is down, we still log out locally
        HttpContext.Session.Clear();

        ShowSuccess("You have been logged out. See you at the start line!");
        return RedirectToAction("Index", "Home");
    }

    // Organisers land on their dashboard, everyone else on the events page
    private IActionResult RedirectToHome()
    {
        return CurrentRole == Roles.Organiser
            ? RedirectToAction("Index", "Dashboard")
            : RedirectToAction("Index", "Home");
    }
}
