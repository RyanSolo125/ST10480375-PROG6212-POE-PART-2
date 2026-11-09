using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

/// <summary>
/// Stores images in an Azure Blob Storage container.
/// The connection string comes from configuration: AzureStorage:ConnectionString
/// (keep it in User Secrets so it never goes to GitHub).
/// </summary>
public class AzureBlobStorageService : IFileStorageService
{
    private readonly BlobContainerClient? _container;
    private readonly ILogger<AzureBlobStorageService> _logger;
    private bool _containerChecked;

    public AzureBlobStorageService(IConfiguration configuration, ILogger<AzureBlobStorageService> logger)
    {
        _logger = logger;

        string? connectionString = configuration["AzureStorage:ConnectionString"];
        string containerName = configuration["AzureStorage:ContainerName"] ?? "raceday-images";

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger.LogWarning("AzureStorage:ConnectionString is not set - image uploads are disabled.");
            return;
        }

        try
        {
            _container = new BlobContainerClient(connectionString, containerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "The Azure Storage connection string is invalid - image uploads are disabled.");
        }
    }

    public bool IsConfigured => _container != null;

    // Creates the container the first time it is needed (private access - files are served through the API)
    private async Task<BlobContainerClient> GetContainerAsync()
    {
        if (_container == null)
        {
            throw new InvalidOperationException("Azure Blob Storage is not configured.");
        }

        if (!_containerChecked)
        {
            await _container.CreateIfNotExistsAsync();
            _containerChecked = true;
        }

        return _container;
    }

    public async Task<string> UploadAsync(Stream content, string contentType, string extension)
    {
        var container = await GetContainerAsync();

        // A random name stops files with the same name overwriting each other
        string fileName = $"{Guid.NewGuid():N}{extension}";
        BlobClient blob = container.GetBlobClient(fileName);

        await blob.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType });
        return fileName;
    }

    public async Task<StoredFile?> DownloadAsync(string fileName)
    {
        var container = await GetContainerAsync();
        BlobClient blob = container.GetBlobClient(fileName);

        if (!await blob.ExistsAsync())
        {
            return null;
        }

        BlobDownloadResult result = (await blob.DownloadContentAsync()).Value;
        return new StoredFile
        {
            Content = result.Content.ToStream(),
            ContentType = result.Details.ContentType ?? "application/octet-stream"
        };
    }

    public async Task DeleteAsync(string fileName)
    {
        if (_container == null) return;

        var container = await GetContainerAsync();
        await container.GetBlobClient(fileName).DeleteIfExistsAsync();
    }
}
