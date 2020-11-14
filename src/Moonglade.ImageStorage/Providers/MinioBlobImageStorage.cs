using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.Exceptions;

namespace Moonglade.ImageStorage.Providers
{
    public class MinioBlobImageStorage : IBlogImageStorage
    {
        public string Name => nameof(MinioBlobImageStorage);

        private readonly MinioClient _client;
        private readonly string _bucketName;

        private readonly ILogger<MinioBlobImageStorage> _logger;

        public MinioBlobImageStorage(ILogger<MinioBlobImageStorage> logger, MinioBlobConfiguration blobConfiguration)
        {
            try
            {
                _logger = logger;

                _client = new(blobConfiguration.EndPoint, blobConfiguration.AccessKey, blobConfiguration.SecretKey);
                if (blobConfiguration.WithSSL)
                {
                    _client = _client.WithSSL();
                }
                _bucketName = blobConfiguration.BucketName;

                logger.LogInformation($"Created {nameof(MinioBlobImageStorage)} at {blobConfiguration.EndPoint}");
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Failed to create {nameof(MinioBlobImageStorage)}");
                throw;
            }
        }

        protected virtual async Task CreateBucketIfNotExists()
        {
            if (!await _client.BucketExistsAsync(_bucketName))
            {
                await _client.MakeBucketAsync(_bucketName);
            }
        }

        public async Task<string> InsertAsync(string fileName, byte[] imageBytes)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    throw new ArgumentNullException(nameof(fileName));
                }

                _logger.LogInformation($"Uploading {fileName} to Minio Blob Storage.");

                await CreateBucketIfNotExists();

                await using var fileStream = new MemoryStream(imageBytes);
                await _client.PutObjectAsync(_bucketName, fileName, fileStream, fileStream.Length);

                _logger.LogInformation($"Uploaded image file '{fileName}' to Minio Blob Storage.");

                return fileName;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error uploading file {fileName} to Minio.");
                throw;
            }
        }

        public async Task DeleteAsync(string fileName)
        {
            try
            {
                if (await BlobExistsAsync(fileName))
                {
                    await _client.RemoveObjectAsync(_bucketName, fileName);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error deleting file {fileName} on Minio, it must be my problem, not Microsoft.");
                throw;
            }
        }

        private async Task<bool> BlobExistsAsync(string fileName)
        {
            // Make sure Blob Container exists.
            if (await _client.BucketExistsAsync(_bucketName))
            {
                try
                {
                    await _client.StatObjectAsync(_bucketName, fileName);
                }
                catch (Exception e)
                {
                    if (e is ObjectNotFoundException)
                    {
                        return false;
                    }
                    throw;
                }
                return true;
            }
            return false;
        }

        public async Task<ImageInfo> GetAsync(string fileName)
        {
            await using var memoryStream = new MemoryStream();
            var extension = Path.GetExtension(fileName);
            if (string.IsNullOrWhiteSpace(extension))
            {
                throw new ArgumentException("File extension is empty");
            }

            try
            {
                var exists = await BlobExistsAsync(fileName);
                if (!exists)
                {
                    _logger.LogWarning($"Blob {fileName} not exist.");
                    return null;
                }

                await _client.GetObjectAsync(_bucketName, fileName, (stream) =>
                {
                    if (stream is not null) stream.CopyTo(memoryStream);
                });
                var arr = memoryStream.ToArray();

                var fileType = extension.Replace(".", string.Empty);
                var imageInfo = new ImageInfo
                {
                    ImageBytes = arr,
                    ImageExtensionName = fileType
                };

                return imageInfo;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error getting image '{fileName}' in minio.");
                throw;
            }
        }
    }
}
