using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace Moonglade.ImageStorage.Providers;

public class FileSystemImageStorage : IBlogImageStorage
{
    public string Name => nameof(FileSystemImageStorage);

    public bool UseCdn => false;

    private readonly ILogger<FileSystemImageStorage> _logger;

    private readonly string _path;

    public FileSystemImageStorage(ILogger<FileSystemImageStorage> logger, FileSystemImageConfiguration imgConfig)
    {
        _logger = logger;
        _path = imgConfig.Path;
    }

    public async Task<ImageInfo> GetAsync(string fileName)
    {
        var imagePath = Path.Join(_path, fileName);

        if (!File.Exists(imagePath))
        {
            // Can not throw FileNotFoundException,
            // because hackers may request a large number of 404 images
            // to flood .NET runtime with exceptions and take out the server
            return null;
        }

        var extension = Path.GetExtension(imagePath);

        var fileType = extension.Replace(".", string.Empty);
        var imageBytes = await ReadFileAsync(imagePath);

        var imageInfo = new ImageInfo
        {
            ImageBytes = imageBytes,
            ImageExtensionName = fileType
        };

        return imageInfo;
    }

    public async Task DeleteAsync(string fileName)
    {
        await Task.CompletedTask;
        var imagePath = Path.Join(_path, fileName);
        if (File.Exists(imagePath))
        {
            File.Delete(imagePath);
        }
    }

    private static async Task<byte[]> ReadFileAsync(string filename)
    {
        await using var file = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        var buff = new byte[file.Length];
        await file.ReadAsync(buff.AsMemory(0, (int)file.Length));
        return buff;
    }

    public async Task<string> InsertAsync(string fileName, byte[] imageBytes)
    {
        var fullPath = Path.Join(_path, fileName);

        await using var sourceStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None,
            4096, true);
        await sourceStream.WriteAsync(imageBytes.AsMemory(0, imageBytes.Length));

        return fileName;
    }

    public static string ResolveImageStoragePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentNullException(nameof(path));
        }

        // Handle Path for non-Windows environment #412
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || Path.DirectorySeparatorChar != '\\')
        {
            if (path.IndexOf('\\') > 0)
            {
                path = path.Replace('\\', Path.DirectorySeparatorChar);
            }
        }

        // IsPathFullyQualified can't check if path is valid, e.g.:
        // Path: C:\Documents<>|foo
        //   Rooted: True
        //   Fully qualified: True
        //   Full path: C:\Documents<>|foo
        var invalidChars = Path.GetInvalidPathChars();
        if (path.IndexOfAny(invalidChars) >= 0)
        {
            throw new InvalidOperationException("Path can not contain invalid chars.");
        }
        if (!Path.IsPathFullyQualified(path))
        {
            throw new InvalidOperationException("Path is not fully qualified.");
        }

        var fullPath = Path.GetFullPath(path);
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }
        return fullPath;
    }
}