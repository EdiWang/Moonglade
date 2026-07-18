using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace Moonglade.ImageStorage.Providers;

/// <summary>
/// Configuration record for file system image storage settings.
/// </summary>
/// <param name="Path">The file system path where images will be stored.</param>
public record FileSystemImageConfiguration(string Path);

/// <summary>
/// Provides file system-based image storage implementation for blog images.
/// Stores images as files in a specified directory on the local file system.
/// </summary>
/// <param name="imgConfig">Configuration containing the storage path.</param>
/// <param name="logger">Logger instance for tracking operations.</param>
public class FileSystemImageStorage(FileSystemImageConfiguration imgConfig, ILogger<FileSystemImageStorage> logger) : IBlogImageStorage
{
    /// <summary>
    /// Gets the name of this image storage provider.
    /// </summary>
    public string Name => nameof(FileSystemImageStorage);

    private readonly string _path = imgConfig.Path;

    /// <summary>
    /// Gets the default storage path for images when no custom path is specified.
    /// Returns a path in the user's profile directory under "moonglade/images".
    /// </summary>
    public static string DefaultPath
    {
        get
        {
            return Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "moonglade", "images");
        }
    }

    public async Task<ImageInfo> GetInfoAsync(string fileName)
    {
        await Task.CompletedTask;
        ValidateFileName(fileName);
        var imagePath = Path.Join(_path, fileName);

        if (!File.Exists(imagePath))
        {
            // Can not throw FileNotFoundException,
            // because hackers may request a large number of 404 images
            // to flood .NET runtime with exceptions and take out the server
            return null;
        }

        var fileInfo = new FileInfo(imagePath);
        var extension = Path.GetExtension(imagePath);
        var fileType = extension.TrimStart('.');

        var imageInfo = new ImageInfo
        {
            ImageExtensionName = fileType,
            ContentType = ImageInfo.GetContentType(fileType),
            ContentLength = fileInfo.Length,
            LastModifiedUtc = fileInfo.LastWriteTimeUtc,
            EntityTag = CreateEntityTag(fileInfo)
        };

        return imageInfo;
    }

    public async Task<Stream> OpenReadAsync(string fileName)
    {
        await Task.CompletedTask;
        ValidateFileName(fileName);
        var imagePath = Path.Join(_path, fileName);

        if (!File.Exists(imagePath))
        {
            return null;
        }

        return new FileStream(
            imagePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
    }

    /// <summary>
    /// Deletes an image file from the file system storage.
    /// </summary>
    /// <param name="fileName">The name of the image file to delete.</param>
    /// <returns>A completed task.</returns>
    /// <remarks>
    /// If the file does not exist, the operation completes silently without error.
    /// </remarks>
    public async Task DeleteAsync(string fileName)
    {
        await Task.CompletedTask;
        ValidateFileName(fileName);
        var imagePath = Path.Join(_path, fileName);
        if (File.Exists(imagePath))
        {
            File.Delete(imagePath);
            logger.LogInformation("Deleted image: {FileName}", fileName);
        }
    }

    /// <summary>
    /// Validates that a file name is safe and does not contain path traversal attempts.
    /// </summary>
    /// <param name="fileName">The file name to validate.</param>
    /// <exception cref="ArgumentException">Thrown when the file name is invalid or contains path traversal characters.</exception>
    private static void ValidateFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));
        }

        // Prevent path traversal attacks by ensuring fileName is just a filename without path components
        var sanitizedFileName = Path.GetFileName(fileName);
        if (sanitizedFileName != fileName)
        {
            throw new ArgumentException("File name contains invalid path characters.", nameof(fileName));
        }

        // Additional check for path traversal sequences
        if (fileName.Contains("..", StringComparison.Ordinal))
        {
            throw new ArgumentException("File name contains path traversal sequences.", nameof(fileName));
        }

        // Check for invalid filename characters
        var invalidChars = Path.GetInvalidFileNameChars();
        if (fileName.IndexOfAny(invalidChars) >= 0)
        {
            throw new ArgumentException("File name contains invalid characters.", nameof(fileName));
        }
    }

    private static string CreateEntityTag(FileInfo fileInfo)
        => $"\"{fileInfo.LastWriteTimeUtc.Ticks:x}-{fileInfo.Length:x}\"";


    /// <summary>
    /// Saves an image to the file system storage.
    /// </summary>
    /// <param name="fileName">The name to use for the saved image file.</param>
    /// <param name="imageBytes">The image data as a byte array.</param>
    /// <returns>The filename of the saved image.</returns>
    /// <remarks>
    /// Creates or overwrites the file if it already exists.
    /// </remarks>
    public async Task<string> InsertAsync(string fileName, byte[] imageBytes)
    {
        ValidateFileName(fileName);
        var fullPath = Path.Join(_path, fileName);

        await using var sourceStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None,
            4096, true);
        await sourceStream.WriteAsync(imageBytes.AsMemory(0, imageBytes.Length));

        logger.LogInformation("Saved image: {FileName}", fileName);

        return fileName;
    }

    /// <summary>
    /// Saves a secondary copy of an image to the file system storage.
    /// </summary>
    /// <param name="fileName">The name to use for the saved image file.</param>
    /// <param name="imageBytes">The image data as a byte array.</param>
    /// <returns>The filename of the saved image.</returns>
    /// <remarks>
    /// This implementation delegates to <see cref="InsertAsync"/> as file system
    /// storage doesn't distinguish between primary and secondary storage.
    /// </remarks>
    public Task<string> InsertSecondaryAsync(string fileName, byte[] imageBytes) => InsertAsync(fileName, imageBytes);

    /// <summary>
    /// Resolves and validates an image storage path, ensuring it's properly formatted
    /// for the current operating system and creating the directory if it doesn't exist.
    /// </summary>
    /// <param name="path">The path to resolve and validate.</param>
    /// <returns>The fully qualified, validated path.</returns>
    /// <exception cref="ArgumentNullException">Thrown when path is null or whitespace.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the path contains invalid characters or is not fully qualified.
    /// </exception>
    /// <remarks>
    /// Handles cross-platform path formatting and creates the directory structure if needed.
    /// </remarks>
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
