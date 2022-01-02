using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Pzy.Qiniu;

namespace Moonglade.ImageStorage.Providers
{
    public class QiniuBlobImageStorage : IBlogImageStorage
    {
        private readonly IMemoryCache _cache;
        private readonly FormUploader _formUploader;
        private readonly Signature _signature;
        private readonly HttpManager _httpManager;
        private readonly BucketManager _bucketManager;
        private readonly ILogger<QiniuBlobImageStorage> _logger;
        private readonly IQiniuConfiguration _qiniuBlobConfiguration;
        /// <summary>
        /// Upload policy expiration time
        /// </summary>
        private const int ExpireSeconds = 3600;
        private const string BlobTokenKey = "qiniu:blob:token";

        public string Name => nameof(QiniuBlobImageStorage);

        public bool UseCdn => true;

        public QiniuBlobImageStorage(ILogger<QiniuBlobImageStorage> logger,
            IMemoryCache cache,
            Signature signature,
            BucketManager bucketManager,
            FormUploader formUploader,
            HttpManager httpManager,
            IQiniuConfiguration blobConfiguration)
        {
            _qiniuBlobConfiguration = blobConfiguration;
            _cache = cache;
            _formUploader = formUploader;
            _signature = signature;
            _bucketManager = bucketManager;
            _logger = logger;
            _httpManager = httpManager;

            logger.LogInformation($"Created {nameof(QiniuBlobImageStorage)} at {blobConfiguration.EndPoint}");
        }

        public async Task<string> InsertAsync(string fileName, byte[] imageBytes)
        {
            await using (var stream = new MemoryStream(imageBytes))
            {
                await SaveFileAsync(stream, fileName);
            }

            _logger.LogInformation($"Uploaded image file '{fileName}' to Qiniu Cloud Storage.");

            return GetCdnUrl(fileName);
        }

        private async Task SaveFileAsync(Stream source, string fileName)
        {
            var blobToken = _cache.GetOrCreate(BlobTokenKey, entry =>
            {
                _logger.LogTrace($"Qiniu blob token not on cache, fetching token...");

                entry.SlidingExpiration = TimeSpan.FromSeconds(ExpireSeconds);
                return GetQiniuBlobToken();
            });
            await _formUploader.UploadStreamAsync(source, fileName, blobToken, null);
        }


        /// <summary>
        /// Get upload token
        /// </summary>
        /// <returns></returns>
        private string GetQiniuBlobToken()
        {
            // set upload policy, ref: https://developer.qiniu.com/kodo/manual/1206/put-policy
            var putPolicy = new PutPolicy
            {
                // set upload target space
                Scope = _qiniuBlobConfiguration.BucketName
            };

            //Use cold storage to save resources
            //putPolicy.FileType = 1;

            // upload policy expiration time (seconds)
            putPolicy.SetExpires(ExpireSeconds);

            return _signature.SignWithData(putPolicy.ToJsonString());
        }

        public async Task DeleteAsync(string fileName)
        {
            await _bucketManager.DeleteAsync(_qiniuBlobConfiguration.BucketName, fileName);
        }

        public async Task<ImageInfo> GetAsync(string fileName)
        {
            await using var memoryStream = new MemoryStream();
            var extension = Path.GetExtension(fileName);
            if (string.IsNullOrWhiteSpace(extension))
            {
                throw new ArgumentException("File extension is empty");
            }

            var deadline = (int)(DateTime.UtcNow.AddMinutes(15) - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds;
            var blobToken = _cache.GetOrCreate(BlobTokenKey, entry =>
            {
                _logger.LogTrace($"Qiniu blob token not on cache, fetching token...");

                entry.SlidingExpiration = TimeSpan.FromSeconds(ExpireSeconds);
                return GetQiniuBlobToken();
            });
            var url = $"{GetCdnUrl(fileName)}?e={deadline}&token={blobToken}";

            var result = await _httpManager.GetAsync(url, blobToken, true);

            var fileType = extension.Replace(".", string.Empty);
            var imageInfo = new ImageInfo
            {
                ImageBytes = result.Data,
                ImageExtensionName = fileType
            };

            return imageInfo;
        }

        private string GetCdnUrl(string fileName)
        {
            var uriSchema = _qiniuBlobConfiguration.UseHttps ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;

            return $"{uriSchema}://{_qiniuBlobConfiguration.EndPoint}/{fileName}";
        }
    }
}
