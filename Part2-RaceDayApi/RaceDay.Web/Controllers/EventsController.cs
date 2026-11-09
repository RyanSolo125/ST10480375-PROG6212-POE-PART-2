using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Public event pages (anyone can view).</summary>
public class EventsController : BaseController
{
    public EventsController(RaceDayApiClient api) : base(api) { }

    /// <summary>Full event information, categories, enrol form and (after the race) the results.</summary>
    public async Task<IActionResult> Details(int id)
    {
        var eventResult = await Api.GetEventAsync(id);
        if (!eventResult.Success) return ApiFailure(eventResult, RedirectToAction("Index", "Home"));

        var ev = eventResult.Data!;
        var model = new EventDetailsViewModel
        {
            Event = ev,
            IsLoggedIn = IsLoggedIn,
            IsParticipant = CurrentRole == Roles.Participant,
            IsOwner = CurrentRole == Roles.Organiser && ev.OrganiserId == CurrentUserId
        };

        // Has this participant already entered this event?
        if (model.IsParticipant)
        {
            var myEnrolments = await Api.GetMyEnrolmentsAsync();
            if (myEnrolments.Success)
            {
                model.MyEnrolment = myEnrolments.Data!.FirstOrDefault(en => en.EventId == id);
            }
        }

        // Leaderboard for events that have already happened
        if (!ev.IsUpcoming)
        {
            var results = await Api.GetEventResultsAsync(id);
            if (results.Success)
            {
                model.Results = results.Data!;
            }
        }

        return View(model);
    }
}
