using RaceDay.Web.Helpers;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>
/// Base class for all Organiser pages. [RequireLogin] blocks anyone who is not a logged-in Organiser.
/// </summary>
[RequireLogin(Role = Roles.Organiser)]
public abstract class OrganiserBaseController : BaseController
{
    protected OrganiserBaseController(RaceDayApiClient api) : base(api) { }

    /// <summary>
    /// Loads one of the logged-in organiser's OWN events (from GET /api/events/mine).
    /// If the event belongs to someone else the API won't return it, so we get "not found".
    /// </summary>
    protected async Task<ApiResult<EventDto>> GetMyEventAsync(int eventId)
    {
        var result = await Api.GetMyEventsAsync();
        if (!result.Success)
        {
            return new ApiResult<EventDto> { StatusCode = result.StatusCode, ErrorMessage = result.ErrorMessage };
        }

        var ev = result.Data!.FirstOrDefault(e => e.EventId == eventId);
        if (ev == null)
        {
            return new ApiResult<EventDto> { StatusCode = 404, ErrorMessage = "Event not found." };
        }

        return new ApiResult<EventDto> { Success = true, StatusCode = 200, Data = ev };
    }

    /// <summary>The first validation error on the page, for showing as a message.</summary>
    protected string FirstModelError()
    {
        return ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault()
               ?? "Please check the values you entered.";
    }
}
