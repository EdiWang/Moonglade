using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;

namespace Moonglade.ImageStorage.Providers;

public record AzureBlobConfiguration(
    string ConnectionString,
    string ContainerName,
    string SecondaryContainerName = null
);

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
        _logger.LogInformation("Initialized container '{ContainerName}' for account '{AccountName}'.", containerName, container.AccountName);
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
            throw new InvalidOperationException("Secondary container is not configured.");
        }

        return await InsertInternalAsync(_secondaryContainer, fileName, imageBytes).ConfigureAwait(false);
    }

    private async Task<string> InsertInternalAsync(BlobContainerClient container, string fileName, byte[] imageBytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(imageBytes);

        if (imageBytes.Length == 0)
        {
            throw new ArgumentException("Image bytes cannot be empty.", nameof(imageBytes));
        }

        _logger.LogInformation("Uploading '{FileName}' to Azure Blob Storage.", fileName);

        try
        {
            var blob = container.GetBlobClient(fileName);
            var blobHttpHeader = new BlobHttpHeaders
            {
                ContentType = GetContentType(Path.GetExtension(fileName))
            };

            await using var fileStream = new MemoryStream(imageBytes);
            var uploadedBlob = await blob.UploadAsync(fileStream, blobHttpHeader).ConfigureAwait(false);

            _logger.LogInformation("Uploaded '{FileName}' to Azure Blob Storage. ETag: '{ETag}'.", fileName, uploadedBlob.Value.ETag);

            return fileName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload '{FileName}' to Azure Blob Storage.", fileName);
            throw;
        }
    }

    private static string GetContentType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".bmp" => "image/bmp",
            ".tiff" or ".tif" => "image/tiff",
            ".ico" => "image/x-icon",
            _ => "application/octet-stream"
        };
    }

    public async Task DeleteAsync(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        try
        {
            _logger.LogInformation("Deleting blob '{FileName}' from Azure Blob Storage.", fileName);
            var response = await _container.DeleteBlobIfExistsAsync(fileName).ConfigureAwait(false);

            if (response.Value)
            {
                _logger.LogInformation("Successfully deleted blob '{FileName}' from Azure Blob Storage.", fileName);
            }
            else
            {
                _logger.LogWarning("Blob '{FileName}' did not exist during deletion attempt.", fileName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete blob '{FileName}' from Azure Blob Storage.", fileName);
            throw;
        }
    }

    public async Task<ImageInfo> GetAsync(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            _logger.LogError("File extension is empty for '{FileName}'.", fileName);
            throw new ArgumentException("File extension is empty.", nameof(fileName));
        }

        try
        {
            var blobClient = _container.GetBlobClient(fileName);

            if (!await blobClient.ExistsAsync().ConfigureAwait(false))
            {
                _logger.LogWarning("Blob '{FileName}' does not exist.", fileName);
                return null;
            }

            _logger.LogInformation("Fetching blob '{FileName}' from Azure Blob Storage.", fileName);
            await using var memoryStream = new MemoryStream();
            await blobClient.DownloadToAsync(memoryStream).ConfigureAwait(false);

            return new ImageInfo
            {
                ImageBytes = memoryStream.ToArray(),
                ImageExtensionName = extension.TrimStart('.')
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch blob '{FileName}' from Azure Blob Storage.", fileName);
            throw;
        }
    }
}
