using System;
using System.IO;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;
using Microsoft.Extensions.Logging;

namespace Moonglade.ImageStorage.Providers
{
    public class FileSystemImageStorage : IBlogImageStorage
    {
        public string Name => nameof(FileSystemImageStorage);

        private readonly ILogger<FileSystemImageStorage> _logger;

        private readonly string _path;

        public FileSystemImageStorage(ILogger<FileSystemImageStorage> logger, FileSystemImageConfiguration imgConfig)
        {
            _logger = logger;
            _path = imgConfig.Path;
        }

        public async Task<Response<ImageInfo>> GetAsync(string fileName)
        {
            try
            {
                var imagePath = Path.Join(_path, fileName);

                if (!File.Exists(imagePath))
                {
                    return new FailedResponse<ImageInfo>((int)ImageResponseCode.ImageNotExistInFileSystem);
                }

                var extension = Path.GetExtension(imagePath);

                var fileType = extension.Replace(".", string.Empty);
                var imageBytes = await ReadFileAsync(imagePath);

                var imageInfo = new ImageInfo
                {
                    ImageBytes = imageBytes,
                    ImageExtensionName = fileType
                };

                return new SuccessResponse<ImageInfo>(imageInfo);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error getting image file {fileName}");
                return new FailedResponse<ImageInfo>((int)ImageResponseCode.GeneralException)
                {
                    Exception = e,
                    Message = e.Message
                };
            }
        }

        public async Task<Response> DeleteAsync(string fileName)
        {
            try
            {
                await Task.CompletedTask;
                var imagePath = Path.Join(_path, fileName);
                if (File.Exists(imagePath))
                {
                    File.Delete(imagePath);
                    return new SuccessResponse();
                }
                return new FailedResponse<ImageInfo>((int)ImageResponseCode.ImageNotExistInFileSystem);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error deleting image file {fileName}");
                return new FailedResponse<ImageInfo>((int)ImageResponseCode.GeneralException)
                {
                    Exception = e,
                    Message = e.Message
                };
            }
        }

        private static async Task<byte[]> ReadFileAsync(string filename)
        {
            await using var file = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
            var buff = new byte[file.Length];
            await file.ReadAsync(buff, 0, (int)file.Length);
            return buff;
        }

        public async Task<Response<string>> InsertAsync(string fileName, byte[] imageBytes)
        {
            try
            {
                var fullPath = Path.Join(_path, fileName);

                await using (var sourceStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None,
                    4096, true))
                {
                    await sourceStream.WriteAsync(imageBytes, 0, imageBytes.Length);
                }

                return new SuccessResponse<string>(fileName);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error writing image file {fileName}");
                return new FailedResponse<string>((int)ImageResponseCode.GeneralException)
                {
                    Exception = e,
                    Message = e.Message
                };
            }
        }
    }
}
