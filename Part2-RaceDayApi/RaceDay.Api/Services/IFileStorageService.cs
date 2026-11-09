namespace RaceDay.Api.Services;

/// <summary>A file read back from storage.</summary>
public class StoredFile
{
    public Stream Content { get; set; } = Stream.Null;
    public string ContentType { get; set; } = "application/octet-stream";
}

/// <summary>
/// Saves and loads image files. The real version uses Azure Blob Storage;
/// the unit tests use a fake in-memory version.
/// </summary>
public interface IFileStorageService
{
    /// <summary>True when storage has been set up (connection string present).</summary>
    bool IsConfigured { get; }

    /// <summary>Uploads a file and returns the unique file name it was saved as.</summary>
    Task<string> UploadAsync(Stream content, string contentType, string extension);

    /// <summary>Downloads a file, or returns null if it doesn't exist.</summary>
    Task<StoredFile?> DownloadAsync(string fileName);

    /// <summary>Deletes a file (does nothing if it doesn't exist).</summary>
    Task DeleteAsync(string fileName);
}
