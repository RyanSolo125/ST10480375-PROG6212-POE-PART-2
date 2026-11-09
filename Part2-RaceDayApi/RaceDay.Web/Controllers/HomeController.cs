using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : BaseController
{
    public HomeController(RaceDayApiClient api) : base(api) { }

    /// <summary>Home page: all upcoming events with search and type filters.</summary>
    public async Task<IActionResult> Index(string? search, string? type)
    {
        var model = new HomeViewModel { Search = search, Type = type };

        var result = await Api.GetEventsAsync(type, search, upcomingOnly: true);
        if (result.Success)
        {
            model.Events = result.Data ?? new();
        }
        else
        {
            // Show the page anyway, with a message instead of the events
            ViewBag.LoadError = result.ErrorMessage;
        }

        return View(model);
    }

    public IActionResult AccessDenied()
    {
        Response.StatusCode = 403;
        return View();
    }

    /// <summary>Friendly page for 404 and other status codes.</summary>
    [Route("Home/StatusCode/{code:int}")]
    public IActionResult HttpStatus(int code)
    {
        var model = code switch
        {
            404 => new ErrorViewModel { StatusCode = 404, Title = "Page not found", Message = "The page or item you're looking for doesn't exist or has been removed." },
            403 => new ErrorViewModel { StatusCode = 403, Title = "Access denied", Message = "You don't have permission to view this page." },
            _ => new ErrorViewModel { StatusCode = code, Title = "Something went wrong", Message = "Please try again in a moment." }
        };

        Response.StatusCode = code;
        return View("Error", model);
    }

    /// <summary>Shown when an unexpected error happens, instead of a raw error page.</summary>
    public IActionResult Error()
    {
        Response.StatusCode = 500;
        return View(new ErrorViewModel { StatusCode = 500 });
    }
}
