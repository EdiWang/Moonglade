using System;
using System.IO;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;
using Microsoft.Extensions.Logging;
using Moonglade.Model;

namespace Moonglade.ImageStorage.FileSystem
{
    public class FileSystemImageProvider : IAsyncImageStorageProvider
    {
        public string Name => nameof(FileSystemImageProvider);

        private readonly ILogger<FileSystemImageProvider> _logger;

        public FileSystemImageProvider(ILogger<FileSystemImageProvider> logger)
        {
            _logger = logger;
            logger.LogInformation($"Created {nameof(FileSystemImageProvider)}");
        }

        public async Task<Response<ImageInfo>> GetAsync(string fileName)
        {
            try
            {
                var imagePath = $@"{AppDomain.CurrentDomain.GetData(Constants.DataDirectory)}\{Constants.FileSystemImageStorageFolder}\{fileName}";

                if (File.Exists(imagePath))
                {
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
                return new FailedResponse<ImageInfo>((int)ResponseFailureCode.ImageNotExistInFileSystem);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error getting image file {fileName}");
                return new FailedResponse<ImageInfo>((int)ResponseFailureCode.GeneralException)
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
                var imagePath = $@"{AppDomain.CurrentDomain.GetData(Constants.DataDirectory)}\{Constants.FileSystemImageStorageFolder}\{fileName}";
                if (File.Exists(imagePath))
                {
                    File.Delete(imagePath);
                    return new SuccessResponse();
                }
                return new FailedResponse<ImageInfo>((int)ResponseFailureCode.ImageNotExistInFileSystem);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error deleting image file {fileName}");
                return new FailedResponse<ImageInfo>((int)ResponseFailureCode.GeneralException)
                {
                    Exception = e,
                    Message = e.Message
                };
            }
        }

        private static async Task<byte[]> ReadFileAsync(string filename)
        {
            using (var file = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
            {
                byte[] buff = new byte[file.Length];
                await file.ReadAsync(buff, 0, (int)file.Length);
                return buff;
            }
        }

        public async Task<Response<string>> InsertAsync(string fileName, byte[] imageBytes)
        {
            try
            {
                fileName = fileName.ToLower().Replace(" ", "-");

                var path = $@"{AppDomain.CurrentDomain.GetData(Constants.DataDirectory)}\{Constants.FileSystemImageStorageFolder}";
                var tp = ResolveConflict(fileName, path);

                //File.WriteAllBytes(kvp.Key, imageBytes);

                using (var sourceStream = new FileStream(tp.Item1, FileMode.Create, FileAccess.Write, FileShare.None,
                    4096, true))
                {
                    await sourceStream.WriteAsync(imageBytes, 0, imageBytes.Length);
                }

                return new SuccessResponse<string>(tp.Item2);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error writing image file {fileName}");
                return new FailedResponse<string>((int)ResponseFailureCode.GeneralException)
                {
                    Exception = e,
                    Message = e.Message
                };
            }
        }

        private static Tuple<string, string> ResolveConflict(string fileName, string path)
        {
            var fullPath = Path.Combine(path, fileName);
            var tempFileName = fileName;

            if (File.Exists(fullPath))
            {
                tempFileName = fileName.Insert(
                    fileName.LastIndexOf('.'), $"_{DateTime.UtcNow:yyyyMMddHHmmss}");
                fullPath = Path.Combine(path, tempFileName);
            }

            return new Tuple<string, string>(fullPath, tempFileName);
        }
    }
}
