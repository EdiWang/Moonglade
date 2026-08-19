using System.Text.RegularExpressions;

namespace Moonglade.Features.SiteVerification;

public static partial class SiteVerificationFileConstants
{
    public const int MaxFileNameLength = 128;
    public const int MaxContentBytes = 64 * 1024;

    private static readonly HashSet<string> ReservedFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "android-icon.png",
        "apple-icon.png",
        "favicon.ico",
        "foaf.xml",
        "manifest.webmanifest",
        "opensearch",
        "robots.txt",
        "sitemap.xml"
    };

    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".txt"] = "text/plain; charset=utf-8",
        [".html"] = "text/html; charset=utf-8",
        [".htm"] = "text/html; charset=utf-8",
        [".xml"] = "application/xml; charset=utf-8",
        [".json"] = "application/json; charset=utf-8"
    };

    public static SiteVerificationFileValidationResult Validate(string fileName, string content)
    {
        var fileNameResult = ValidateFileName(fileName);
        if (!fileNameResult.Succeeded) return fileNameResult;

        if (string.IsNullOrWhiteSpace(content))
        {
            return SiteVerificationFileValidationResult.Failure("File content is required.");
        }

        var contentBytes = Encoding.UTF8.GetByteCount(content);
        if (contentBytes > MaxContentBytes)
        {
            return SiteVerificationFileValidationResult.Failure("File content cannot exceed 64 KB.");
        }

        return SiteVerificationFileValidationResult.Success();
    }

    public static SiteVerificationFileValidationResult ValidateFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return SiteVerificationFileValidationResult.Failure("File name is required.");
        }

        if (fileName.Length > MaxFileNameLength)
        {
            return SiteVerificationFileValidationResult.Failure("File name cannot exceed 128 characters.");
        }

        if (Path.GetFileName(fileName) != fileName)
        {
            return SiteVerificationFileValidationResult.Failure("File name must be a root-level file name.");
        }

        if (fileName.Contains("..", StringComparison.Ordinal))
        {
            return SiteVerificationFileValidationResult.Failure("File name cannot contain path traversal segments.");
        }

        if (!AllowedFileNameRegex().IsMatch(fileName))
        {
            return SiteVerificationFileValidationResult.Failure("File name can only contain letters, digits, dots, underscores, and hyphens.");
        }

        if (fileName.EndsWith(".", StringComparison.Ordinal))
        {
            return SiteVerificationFileValidationResult.Failure("File name cannot end with a dot.");
        }

        if (ReservedFileNames.Contains(fileName))
        {
            return SiteVerificationFileValidationResult.Failure("File name is reserved by the application.");
        }

        if (!ContentTypes.ContainsKey(Path.GetExtension(fileName)))
        {
            return SiteVerificationFileValidationResult.Failure("Unsupported file type.");
        }

        return SiteVerificationFileValidationResult.Success();
    }

    public static string NormalizeFileName(string fileName) => fileName.ToLowerInvariant();

    public static string GetContentType(string fileName) => ContentTypes[Path.GetExtension(fileName)];

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$", RegexOptions.CultureInvariant)]
    private static partial Regex AllowedFileNameRegex();
}

public sealed record SiteVerificationFileValidationResult(bool Succeeded, string ErrorMessage)
{
    public static SiteVerificationFileValidationResult Success() => new(true, null);

    public static SiteVerificationFileValidationResult Failure(string errorMessage) => new(false, errorMessage);
}
