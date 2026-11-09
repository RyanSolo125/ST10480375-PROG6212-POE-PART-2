using RaceDay.Api.Services;

namespace RaceDay.Tests;

/// <summary>
/// Pretends to be Azure Blob Storage by keeping files in a dictionary,
/// so the tests don't need an Azure account or internet.
/// </summary>
public class FakeFileStorage : IFileStorageService
{
    public Dictionary<string, byte[]> Files { get; } = new();
    public List<string> DeletedFiles { get; } = new();

    public bool IsConfigured { get; set; } = true;

    public async Task<string> UploadAsync(Stream content, string contentType, string extension)
    {
        string fileName = $"{Guid.NewGuid():N}{extension}";
        using var memory = new MemoryStream();
        await content.CopyToAsync(memory);
        Files[fileName] = memory.ToArray();
        return fileName;
    }

    public Task<StoredFile?> DownloadAsync(string fileName)
    {
        if (!Files.TryGetValue(fileName, out var bytes))
        {
            return Task.FromResult<StoredFile?>(null);
        }

        return Task.FromResult<StoredFile?>(new StoredFile { Content = new MemoryStream(bytes), ContentType = "image/png" });
    }

    public Task DeleteAsync(string fileName)
    {
        Files.Remove(fileName);
        DeletedFiles.Add(fileName);
        return Task.CompletedTask;
    }
}
