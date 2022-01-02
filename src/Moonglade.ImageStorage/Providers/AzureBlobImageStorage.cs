using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;

namespace Moonglade.ImageStorage.Providers;

public class AzureBlobImageStorage : IBlogImageStorage
{
    public string Name => nameof(AzureBlobImageStorage);

    public bool UseCdn => false;

    private readonly BlobContainerClient _container;

    private readonly ILogger<AzureBlobImageStorage> _logger;

    public AzureBlobImageStorage(ILogger<AzureBlobImageStorage> logger, AzureBlobConfiguration blobConfiguration)
    {
        _logger = logger;

        _container = new(blobConfiguration.ConnectionString, blobConfiguration.ContainerName);

        logger.LogInformation($"Created {nameof(AzureBlobImageStorage)} for account {_container.AccountName} on container {_container.Name}");
    }

    public async Task<string> InsertAsync(string fileName, byte[] imageBytes)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentNullException(nameof(fileName));
        }

        _logger.LogInformation($"Uploading {fileName} to Azure Blob Storage.");


        var blob = _container.GetBlobClient(fileName);

        // Why .NET doesn't have MimeMapping.GetMimeMapping()
        var blobHttpHeader = new BlobHttpHeaders();
        var extension = Path.GetExtension(blob.Uri.AbsoluteUri);
        blobHttpHeader.ContentType = extension.ToLower() switch
        {
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            _ => blobHttpHeader.ContentType
        };

        await using var fileStream = new MemoryStream(imageBytes);
        var uploadedBlob = await blob.UploadAsync(fileStream, blobHttpHeader);

        _logger.LogInformation($"Uploaded image file '{fileName}' to Azure Blob Storage, ETag '{uploadedBlob.Value.ETag}'. Yeah, the best cloud!");

        return fileName;
    }

    public async Task DeleteAsync(string fileName)
    {
        await _container.DeleteBlobIfExistsAsync(fileName);
    }

    public async Task<ImageInfo> GetAsync(string fileName)
    {
        var blobClient = _container.GetBlobClient(fileName);
        await using var memoryStream = new MemoryStream();
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            throw new ArgumentException("File extension is empty");
        }

        var existsTask = blobClient.ExistsAsync();
        var downloadTask = blobClient.DownloadToAsync(memoryStream);

        var exists = await existsTask;
        if (!exists)
        {
            _logger.LogWarning($"Blob {fileName} not exist.");

            // Can not throw FileNotFoundException,
            // because hackers may request a large number of 404 images
            // to flood .NET runtime with exceptions and take out the server
            return null;
        }

        await downloadTask;
        var arr = memoryStream.ToArray();

        var fileType = extension.Replace(".", string.Empty);
        var imageInfo = new ImageInfo
        {
            ImageBytes = arr,
            ImageExtensionName = fileType
        };

        return imageInfo;
    }
}