using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>
/// Shows images (event banners, profile pictures).
/// The image is fetched from the API, which reads it from Azure Blob Storage -
/// this app never connects to Azure itself.
/// </summary>
public class ImagesController : BaseController
{
    public ImagesController(RaceDayApiClient api) : base(api) { }

    [HttpGet("images/{fileName}")]
    [ResponseCache(Duration = 3600)]
    public async Task<IActionResult> Get(string fileName)
    {
        var result = await Api.GetImageAsync(fileName);
        if (!result.Success) return NotFound();

        return File(result.Data!.Bytes, result.Data.ContentType);
    }
}
