using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace Moonglade.ImageStorage.Providers;

public class MinioBlobImageStorage : IBlogImageStorage
{
    public string Name => nameof(MinioBlobImageStorage);

    private readonly IMinioClient _client;
    private readonly string _bucketName;

    private readonly ILogger<MinioBlobImageStorage> _logger;

    public MinioBlobImageStorage(ILogger<MinioBlobImageStorage> logger, MinioBlobConfiguration blobConfiguration)
    {
        _logger = logger;

        _client = new MinioClient()
            .WithEndpoint(blobConfiguration.EndPoint)
            .WithCredentials(blobConfiguration.AccessKey, blobConfiguration.SecretKey);
        if (blobConfiguration.WithSSL)
        {
            _client = _client.WithSSL();
        }
        _client.Build();

        _bucketName = blobConfiguration.BucketName;

        logger.LogInformation($"Created {nameof(MinioBlobImageStorage)} at {blobConfiguration.EndPoint}");
    }

    protected virtual async Task CreateBucketIfNotExists()
    {
        var arg = new BucketExistsArgs().WithBucket(_bucketName);
        if (!await _client.BucketExistsAsync(arg))
        {
            var arg1 = new MakeBucketArgs().WithBucket(_bucketName);
            await _client.MakeBucketAsync(arg1);
        }
    }

    public async Task<string> InsertAsync(string fileName, byte[] imageBytes)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentNullException(nameof(fileName));
        }

        _logger.LogInformation($"Uploading {fileName} to Minio Blob Storage.");

        await CreateBucketIfNotExists();

        await using var fileStream = new MemoryStream(imageBytes);

        var putObjectArg = new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithFileName(fileName)
            .WithStreamData(fileStream)
            .WithObjectSize(fileStream.Length);

        await _client.PutObjectAsync(putObjectArg);

        _logger.LogInformation($"Uploaded image file '{fileName}' to Minio Blob Storage.");

        return fileName;
    }

    public async Task DeleteAsync(string fileName)
    {
        if (await BlobExistsAsync(fileName))
        {
            await _client.RemoveObjectAsync(
                new RemoveObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(fileName));
        }
    }

    private async Task<bool> BlobExistsAsync(string fileName)
    {
        // Make sure Blob Container exists.
        if (!await _client.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucketName))) return false;

        try
        {
            var arg = new StatObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(fileName);

            await _client.StatObjectAsync(arg);
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

    public async Task<ImageInfo> GetAsync(string fileName)
    {
        await using var memoryStream = new MemoryStream();
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            throw new ArgumentException("File extension is empty");
        }

        var exists = await BlobExistsAsync(fileName);
        if (!exists)
        {
            _logger.LogWarning($"Blob {fileName} not exist.");
            return null;
        }

        var arg = new GetObjectArgs()
            .WithBucket(_bucketName)
            .WithFile(fileName)
            .WithCallbackStream(stream =>
        {
            stream?.CopyTo(memoryStream);
        });

        await _client.GetObjectAsync(arg);
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