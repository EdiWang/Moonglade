using System;
using System.IO;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Logging;
using Moonglade.Model;

namespace Moonglade.ImageStorage.AzureBlob
{
    public class AzureStorageImageProvider : IAsyncImageStorageProvider
    {
        public string Name => nameof(AzureStorageImageProvider);

        private readonly CloudBlobContainer _container;

        private readonly ILogger<AzureStorageImageProvider> _logger;

        public AzureStorageImageProvider(ILogger<AzureStorageImageProvider> logger, AzureStorageInfo storageInfo)
        {
            try
            {
                _logger = logger;

                var storageAccount = CloudStorageAccount.Parse(storageInfo.ConnectionString);
                var blobClient = storageAccount.CreateCloudBlobClient();
                _container = blobClient.GetContainerReference(storageInfo.ContainerName);

                logger.LogInformation($"Created {nameof(AzureStorageImageProvider)} for account {storageAccount.BlobEndpoint} on container {_container.Name}");
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

                var ext = Path.GetExtension(fileName);
                var newFileName = $"img-{Guid.NewGuid()}{ext}";
                _logger.LogInformation($"Uploading {newFileName} to Azure Blob Storage.");

                var blockBlob = _container.GetBlockBlobReference(newFileName);
                using (var fileStream = new MemoryStream(imageBytes))
                {
                    await blockBlob.UploadFromStreamAsync(fileStream);
                }

                _logger.LogInformation($"Uploaded image file {newFileName} to Azure Blob Storage! Yeah, the best cloud!");

                return new SuccessResponse<string>(newFileName);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error uploading file {fileName} to Azure, it must be my problem, not Microsoft.");
                return new FailedResponse<string>((int) ResponseFailureCode.GeneralException, e.Message, e);
            }
        }

        public async Task<Response> DeleteAsync(string fileName)
        {
            try
            {
                var blockBlob = _container.GetBlockBlobReference(fileName);
                var exists = await blockBlob.ExistsAsync();
                if (exists)
                {
                    await blockBlob.DeleteAsync();
                    return new SuccessResponse();
                }
                return new FailedResponse((int)ResponseFailureCode.ImageNotExistInAzureBlob);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error deleting file {fileName} on Azure, it must be my problem, not Microsoft.");
                return new FailedResponse((int)ResponseFailureCode.GeneralException)
                {
                    Exception = e,
                    Message = e.Message
                };
            }
        }

        public async Task<Response<ImageInfo>> GetAsync(string fileName)
        {
            var blockBlob = _container.GetBlockBlobReference(fileName);
            using (var memoryStream = new MemoryStream())
            {
                var extension = Path.GetExtension(fileName);
                if (string.IsNullOrWhiteSpace(extension))
                {
                    return new FailedResponse<ImageInfo>((int)ResponseFailureCode.ExtensionNameIsNull);
                }

                var exists = await blockBlob.ExistsAsync();
                if (!exists)
                {
                    _logger.LogWarning($"Blob {fileName} not exist.");
                    return new FailedResponse<ImageInfo>((int)ResponseFailureCode.ImageNotExistInAzureBlob);
                }

                await blockBlob.DownloadToStreamAsync(memoryStream);
                var arr = memoryStream.ToArray();

                var fileType = extension.Replace(".", string.Empty);
                var imageInfo = new ImageInfo
                {
                    ImageBytes = arr,
                    ImageExtensionName = fileType
                };

                return new SuccessResponse<ImageInfo>(imageInfo);
            }
        }
    }
}
