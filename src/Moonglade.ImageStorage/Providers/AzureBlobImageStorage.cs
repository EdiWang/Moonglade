using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;

namespace Moonglade.ImageStorage.Providers;

public class AzureBlobImageStorage : IBlogImageStorage
{
    public string Name => nameof(AzureBlobImageStorage);

    private readonly BlobContainerClient _container;
    private readonly BlobContainerClient _secondaryContainer;
    private readonly ILogger<AzureBlobImageStorage> _logger;

    public AzureBlobImageStorage(ILogger<AzureBlobImageStorage> logger, AzureBlobConfiguration blobConfiguration)
    {
        _logger = logger;
        _container = InitializeContainer(blobConfiguration.ConnectionString, blobConfiguration.ContainerName);

        if (!string.IsNullOrWhiteSpace(blobConfiguration.SecondaryContainerName))
        {
            _secondaryContainer = InitializeContainer(blobConfiguration.ConnectionString, blobConfiguration.SecondaryContainerName);
        }
    }

    private BlobContainerClient InitializeContainer(string connectionString, string containerName)
    {
        var container = new BlobContainerClient(connectionString, containerName);
        _logger.LogInformation($"Initialized container '{containerName}' for account '{container.AccountName}'.");
        return container;
    }

    public async Task<string> InsertAsync(string fileName, byte[] imageBytes)
    {
        return await InsertInternalAsync(_container, fileName, imageBytes).ConfigureAwait(false);
    }

    public async Task<string> InsertSecondaryAsync(string fileName, byte[] imageBytes)
    {
        if (_secondaryContainer is null)
        {
            _logger.LogError("Secondary container is not configured.");
            return null;
        }

        return await InsertInternalAsync(_secondaryContainer, fileName, imageBytes).ConfigureAwait(false);
    }

    private async Task<string> InsertInternalAsync(BlobContainerClient container, string fileName, byte[] imageBytes)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentNullException(nameof(fileName));
        }

        _logger.LogInformation($"Uploading '{fileName}' to Azure Blob Storage.");

        var blob = container.GetBlobClient(fileName);
        var blobHttpHeader = new BlobHttpHeaders
        {
            ContentType = GetContentType(Path.GetExtension(blob.Uri.AbsoluteUri))
        };

        await using var fileStream = new MemoryStream(imageBytes);
        var uploadedBlob = await blob.UploadAsync(fileStream, blobHttpHeader).ConfigureAwait(false);

        _logger.LogInformation($"Uploaded '{fileName}' to Azure Blob Storage. ETag: '{uploadedBlob.Value.ETag}'.");

        return fileName;
    }

    private string GetContentType(string extension)
    {
        return extension.ToLower() switch
        {
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

    public async Task DeleteAsync(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentNullException(nameof(fileName));
        }

        _logger.LogInformation($"Deleting blob '{fileName}' from Azure Blob Storage.");
        await _container.DeleteBlobIfExistsAsync(fileName).ConfigureAwait(false);
    }

    public async Task<ImageInfo> GetAsync(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentNullException(nameof(fileName));
        }

        var blobClient = _container.GetBlobClient(fileName);
        var extension = Path.GetExtension(fileName);

        if (string.IsNullOrWhiteSpace(extension))
        {
            _logger.LogError("File extension is empty.");
            throw new ArgumentException("File extension is empty.");
        }

        if (!await blobClient.ExistsAsync().ConfigureAwait(false))
        {
            _logger.LogWarning($"Blob '{fileName}' does not exist.");

            // Can not throw FileNotFoundException,
            // because hackers may request a large number of 404 images
            // to flood .NET runtime with exceptions and take out the server
            return null;
        }

        _logger.LogInformation($"Fetching blob '{fileName}' from Azure Blob Storage.");
        await using var memoryStream = new MemoryStream();
        await blobClient.DownloadToAsync(memoryStream).ConfigureAwait(false);

        return new ImageInfo
        {
            ImageBytes = memoryStream.ToArray(),
            ImageExtensionName = extension.TrimStart('.')
        };
    }
}
