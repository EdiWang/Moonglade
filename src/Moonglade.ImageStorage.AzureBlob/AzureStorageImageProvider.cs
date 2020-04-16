using System;
using System.IO;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;

namespace Moonglade.ImageStorage.AzureBlob
{
    public class AzureStorageImageProvider : IAsyncImageStorageProvider
    {
        public string Name => nameof(AzureStorageImageProvider);

        private readonly BlobContainerClient _container;

        private readonly ILogger<AzureStorageImageProvider> _logger;

        public AzureStorageImageProvider(ILogger<AzureStorageImageProvider> logger, AzureStorageInfo storageInfo)
        {
            try
            {
                _logger = logger;

                _container = new BlobContainerClient(storageInfo.ConnectionString, storageInfo.ContainerName);

                logger.LogInformation($"Created {nameof(AzureStorageImageProvider)} for account {_container.AccountName} on container {_container.Name}");
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Failed to create {nameof(AzureStorageImageProvider)}");
                throw;
            }
        }

        public async Task<Response<string>> InsertAsync(string fileName, byte[] imageBytes)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    throw new ArgumentNullException(nameof(fileName));
                }

                _logger.LogInformation($"Uploading {fileName} to Azure Blob Storage.");


                var blob = _container.GetBlobClient(fileName);

                // Why .NET Core doesn't have MimeMapping.GetMimeMapping()
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

                await using (var fileStream = new MemoryStream(imageBytes))
                {
                    var uploadedBlob = await blob.UploadAsync(fileStream, blobHttpHeader);

                    _logger.LogInformation($"Uploaded image file '{fileName}' to Azure Blob Storage, ETag '{uploadedBlob.Value.ETag}'. Yeah, the best cloud!");
                }


                return new SuccessResponse<string>(fileName);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error uploading file {fileName} to Azure, it must be my problem, not Microsoft.");
                return new FailedResponse<string>((int)ImageResponseCode.GeneralException, e.Message, e);
            }
        }

        public async Task<Response> DeleteAsync(string fileName)
        {
            try
            {
                var ok = await _container.DeleteBlobIfExistsAsync(fileName);
                if (ok)
                {
                    return new SuccessResponse();
                }
                return new FailedResponse((int)ImageResponseCode.ImageNotExistInAzureBlob);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error deleting file {fileName} on Azure, it must be my problem, not Microsoft.");
                return new FailedResponse((int)ImageResponseCode.GeneralException)
                {
                    Exception = e,
                    Message = e.Message
                };
            }
        }

        public async Task<Response<ImageInfo>> GetAsync(string fileName)
        {
            var blobClient = _container.GetBlobClient(fileName);
            await using var memoryStream = new MemoryStream();
            var extension = Path.GetExtension(fileName);
            if (string.IsNullOrWhiteSpace(extension))
            {
                return new FailedResponse<ImageInfo>((int)ImageResponseCode.ExtensionNameIsNull);
            }

            // This needs to try-catch 404 status now :(
            // See https://github.com/Azure/azure-sdk-for-net/issues/8952
            //var exists = await blobClient.ExistsAsync();
            //if (!exists)
            //{
            //    _logger.LogWarning($"Blob {fileName} not exist.");
            //    return new FailedResponse<ImageInfo>((int)ImageResponseCode.ImageNotExistInAzureBlob);
            //}

            try
            {
                await blobClient.DownloadToAsync(memoryStream);
                var arr = memoryStream.ToArray();

                var fileType = extension.Replace(".", string.Empty);
                var imageInfo = new ImageInfo
                {
                    ImageBytes = arr,
                    ImageExtensionName = fileType
                };

                return new SuccessResponse<ImageInfo>(imageInfo);
            }
            catch (Azure.RequestFailedException e)
            {
                if (e.Status == 404)
                {
                    return new FailedResponse<ImageInfo>((int)ImageResponseCode.ImageNotExistInAzureBlob);
                }

                throw;
            }
        }
    }
}
