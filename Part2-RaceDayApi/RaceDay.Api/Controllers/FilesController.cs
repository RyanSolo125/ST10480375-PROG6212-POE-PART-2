using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Helpers;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/files")]
public class FilesController : BaseApiController
{
    private readonly IFileStorageService _storage;

    public FilesController(IFileStorageService storage)
    {
        _storage = storage;
    }

    /// <summary>Returns an image stored in Azure Blob Storage (public).</summary>
    /// <remarks>
    /// Event banners and profile pictures are loaded through this endpoint,
    /// so the MVC app never talks to Azure directly.
    /// </remarks>
    /// <param name="fileName">The file name from bannerFileName or profilePictureFileName.</param>
    /// <response code="200">The image file.</response>
    /// <response code="404">No file with that name.</response>
    [HttpGet("{fileName}")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string fileName)
    {
        if (!ImageRules.IsSafeFileName(fileName) || !_storage.IsConfigured)
        {
            return NotFound(new { message = "File not found." });
        }

        var file = await _storage.DownloadAsync(fileName);
        if (file == null) return NotFound(new { message = "File not found." });

        return File(file.Content, file.ContentType);
    }
}
