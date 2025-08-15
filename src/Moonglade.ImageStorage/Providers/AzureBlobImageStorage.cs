using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;

namespace Moonglade.ImageStorage.Providers;

/// <summary>
/// Configuration record for Azure Blob Storage settings.
/// </summary>
/// <param name="ConnectionString">The connection string for the Azure Storage account.</param>
/// <param name="ContainerName">The name of the primary blob container.</param>
/// <param name="SecondaryContainerName">The optional name of the secondary blob container.</param>
public record AzureBlobConfiguration(
    string ConnectionString,
    string ContainerName,
    string SecondaryContainerName = null
);

/// <summary>
/// Azure Blob Storage implementation of the blog image storage interface.
/// Provides functionality to store, retrieve, and delete images in Azure Blob Storage containers.
/// </summary>
public class AzureBlobImageStorage : IBlogImageStorage
{
    /// <summary>
    /// Gets the name of this storage provider.
    /// </summary>
    public string Name => nameof(AzureBlobImageStorage);

    private readonly BlobContainerClient _container;
    private readonly BlobContainerClient _secondaryContainer;
    private readonly ILogger<AzureBlobImageStorage> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureBlobImageStorage"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for logging operations.</param>
    /// <param name="blobConfiguration">The Azure Blob Storage configuration containing connection details.</param>
    public AzureBlobImageStorage(ILogger<AzureBlobImageStorage> logger, AzureBlobConfiguration blobConfiguration)
    {
        _logger = logger;
        _container = InitializeContainer(blobConfiguration.ConnectionString, blobConfiguration.ContainerName);

        if (!string.IsNullOrWhiteSpace(blobConfiguration.SecondaryContainerName))
        {
            _secondaryContainer = InitializeContainer(blobConfiguration.ConnectionString, blobConfiguration.SecondaryContainerName);
        }
    }

    /// <summary>
    /// Initializes a blob container client with the specified connection string and container name.
    /// </summary>
    /// <param name="connectionString">The Azure Storage account connection string.</param>
    /// <param name="containerName">The name of the blob container to initialize.</param>
    /// <returns>A configured <see cref="BlobContainerClient"/> instance.</returns>
    private BlobContainerClient InitializeContainer(string connectionString, string containerName)
    {
        var container = new BlobContainerClient(connectionString, containerName);
        _logger.LogInformation("Initialized container '{ContainerName}' for account '{AccountName}'.", containerName, container.AccountName);
        return container;
    }

    /// <summary>
    /// Inserts an image into the primary blob container.
    /// </summary>
    /// <param name="fileName">The name of the file to be stored.</param>
    /// <param name="imageBytes">The image data as a byte array.</param>
    /// <returns>A task that represents the asynchronous insert operation. The task result contains the file name of the uploaded image.</returns>
    /// <exception cref="ArgumentException">Thrown when the file name is null, empty, or whitespace, or when image bytes are empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when image bytes are null.</exception>
    public async Task<string> InsertAsync(string fileName, byte[] imageBytes)
    {
        return await InsertInternalAsync(_container, fileName, imageBytes).ConfigureAwait(false);
    }

    /// <summary>
    /// Inserts an image into the secondary blob container.
    /// </summary>
    /// <param name="fileName">The name of the file to be stored.</param>
    /// <param name="imageBytes">The image data as a byte array.</param>
    /// <returns>A task that represents the asynchronous insert operation. The task result contains the file name of the uploaded image.</returns>
    /// <exception cref="ArgumentException">Thrown when the file name is null, empty, or whitespace, or when image bytes are empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when image bytes are null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the secondary container is not configured.</exception>
    public async Task<string> InsertSecondaryAsync(string fileName, byte[] imageBytes)
    {
        if (_secondaryContainer is null)
        {
            _logger.LogError("Secondary container is not configured.");
            throw new InvalidOperationException("Secondary container is not configured.");
        }

        return await InsertInternalAsync(_secondaryContainer, fileName, imageBytes).ConfigureAwait(false);
    }

    /// <summary>
    /// Internal method to insert an image into the specified blob container.
    /// </summary>
    /// <param name="container">The blob container client to upload to.</param>
    /// <param name="fileName">The name of the file to be stored.</param>
    /// <param name="imageBytes">The image data as a byte array.</param>
    /// <returns>A task that represents the asynchronous insert operation. The task result contains the file name of the uploaded image.</returns>
    /// <exception cref="ArgumentException">Thrown when the file name is null, empty, or whitespace, or when image bytes are empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when image bytes are null.</exception>
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

    /// <summary>
    /// Determines the appropriate MIME content type based on the file extension.
    /// </summary>
    /// <param name="extension">The file extension including the leading dot.</param>
    /// <returns>The MIME content type string for the specified file extension.</returns>
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

    /// <summary>
    /// Deletes an image from the primary blob container.
    /// </summary>
    /// <param name="fileName">The name of the file to be deleted.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    /// <exception cref="ArgumentException">Thrown when the file name is null, empty, or whitespace.</exception>
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

    /// <summary>
    /// Retrieves an image from the primary blob container.
    /// </summary>
    /// <param name="fileName">The name of the file to retrieve.</param>
    /// <returns>A task that represents the asynchronous get operation. The task result contains the image information, or null if the image does not exist.</returns>
    /// <exception cref="ArgumentException">Thrown when the file name is null, empty, whitespace, or has no file extension.</exception>
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
