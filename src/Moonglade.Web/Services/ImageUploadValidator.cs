using SkiaSharp;
using System.Xml;
using System.Xml.Linq;

namespace Moonglade.Web.Services;

public interface IImageUploadValidator
{
    ImageUploadValidationResult Validate(string fileName, byte[] imageBytes);
}

public sealed record ImageUploadValidationResult(
    bool Succeeded,
    string Extension,
    string ContentType,
    byte[] ImageBytes,
    string ErrorMessage)
{
    public static ImageUploadValidationResult Success(string extension, string contentType, byte[] imageBytes) =>
        new(true, extension, contentType, imageBytes, null);

    public static ImageUploadValidationResult Failure(string errorMessage) =>
        new(false, null, null, null, errorMessage);
}

public class ImageUploadValidator : IImageUploadValidator
{
    private const int MaxSvgCharacters = 5 * 1024 * 1024;
    private const string SvgNamespace = "http://www.w3.org/2000/svg";

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".gif",
        ".webp",
        ".svg"
    };

    private static readonly HashSet<string> DangerousSvgElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "script",
        "foreignObject",
        "iframe",
        "object",
        "embed",
        "applet",
        "audio",
        "video",
        "source",
        "track",
        "canvas",
        "frame",
        "frameset",
        "base",
        "link",
        "meta",
        "form",
        "input",
        "button",
        "textarea",
        "select",
        "option"
    };

    private static readonly HashSet<string> UriAttributeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "href",
        "src",
        "action",
        "formaction"
    };

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a];

    public ImageUploadValidationResult Validate(string fileName, byte[] imageBytes)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return ImageUploadValidationResult.Failure("File name is required.");
        }

        if (imageBytes is null || imageBytes.Length == 0)
        {
            return ImageUploadValidationResult.Failure("Image file is empty.");
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return ImageUploadValidationResult.Failure("Unsupported image file type.");
        }

        return extension switch
        {
            ".svg" => ValidateSvg(imageBytes),
            ".png" => ValidateRasterImage(extension, imageBytes, HasPngSignature),
            ".jpg" or ".jpeg" => ValidateRasterImage(extension, imageBytes, HasJpegSignature),
            ".gif" => ValidateRasterImage(extension, imageBytes, HasGifSignature),
            ".webp" => ValidateRasterImage(extension, imageBytes, HasWebpSignature),
            _ => ImageUploadValidationResult.Failure("Unsupported image file type.")
        };
    }

    private static ImageUploadValidationResult ValidateRasterImage(
        string extension,
        byte[] imageBytes,
        Func<byte[], bool> hasExpectedSignature)
    {
        if (!hasExpectedSignature(imageBytes))
        {
            return ImageUploadValidationResult.Failure("Image content does not match file extension.");
        }

        if (!CanDecodeImage(imageBytes))
        {
            return ImageUploadValidationResult.Failure("Invalid image data.");
        }

        return ImageUploadValidationResult.Success(extension, ImageInfo.GetContentType(extension), imageBytes);
    }

    private static bool CanDecodeImage(byte[] imageBytes)
    {
        using var stream = new SKMemoryStream(imageBytes);
        using var codec = SKCodec.Create(stream);

        return codec?.Info.Width > 0 && codec.Info.Height > 0;
    }

    private static ImageUploadValidationResult ValidateSvg(byte[] imageBytes)
    {
        XDocument document;

        try
        {
            document = LoadSvgDocument(imageBytes);
        }
        catch (XmlException)
        {
            return ImageUploadValidationResult.Failure("Invalid SVG data.");
        }

        if (document.Root is null ||
            !string.Equals(document.Root.Name.LocalName, "svg", StringComparison.OrdinalIgnoreCase) ||
            !IsAllowedSvgNamespace(document.Root.Name.NamespaceName))
        {
            return ImageUploadValidationResult.Failure("Invalid SVG data.");
        }

        SanitizeSvgDocument(document);

        var sanitizedBytes = WriteSvgDocument(document);
        return ImageUploadValidationResult.Success(".svg", "image/svg+xml", sanitizedBytes);
    }

    private static XDocument LoadSvgDocument(byte[] imageBytes)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = MaxSvgCharacters
        };

        using var stream = new MemoryStream(imageBytes);
        using var reader = XmlReader.Create(stream, settings);

        return XDocument.Load(reader, LoadOptions.PreserveWhitespace);
    }

    private static void SanitizeSvgDocument(XDocument document)
    {
        document.DescendantNodes().OfType<XComment>().Remove();
        document.DescendantNodes().OfType<XProcessingInstruction>().Remove();

        foreach (var element in document.Descendants().ToList())
        {
            if (DangerousSvgElements.Contains(element.Name.LocalName))
            {
                element.Remove();
                continue;
            }

            if (string.Equals(element.Name.LocalName, "style", StringComparison.OrdinalIgnoreCase) &&
                ContainsUnsafeCss(element.Value))
            {
                element.Remove();
                continue;
            }

            foreach (var attribute in element.Attributes().ToList())
            {
                if (ShouldRemoveSvgAttribute(attribute))
                {
                    attribute.Remove();
                }
            }
        }
    }

    private static bool ShouldRemoveSvgAttribute(XAttribute attribute)
    {
        if (attribute.IsNamespaceDeclaration)
        {
            return false;
        }

        var localName = attribute.Name.LocalName;
        var value = attribute.Value;

        if (localName.StartsWith("on", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(localName, "srcdoc", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(localName, "style", StringComparison.OrdinalIgnoreCase))
        {
            return ContainsUnsafeCss(value);
        }

        if (UriAttributeNames.Contains(localName))
        {
            return IsUnsafeSvgUri(value);
        }

        return ContainsUnsafeUriToken(value);
    }

    private static bool IsUnsafeSvgUri(string value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
        {
            return false;
        }

        if (ContainsUnsafeUriToken(trimmed))
        {
            return true;
        }

        if (trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return !IsAllowedImageDataUri(trimmed);
        }

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            return uri.Scheme is not "http" and not "https" and not "mailto";
        }

        var colonIndex = trimmed.IndexOf(':');
        var slashIndex = trimmed.IndexOf('/');
        return colonIndex >= 0 && (slashIndex < 0 || colonIndex < slashIndex);
    }

    private static bool ContainsUnsafeUriToken(string value)
    {
        var normalized = RemoveAsciiWhitespaceAndControlCharacters(value);

        return normalized.Contains("javascript:", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("vbscript:", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("file:", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsUnsafeCss(string value) =>
        value.Contains("url(", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("@import", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("expression(", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("javascript:", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("vbscript:", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("-moz-binding", StringComparison.OrdinalIgnoreCase);

    private static bool IsAllowedImageDataUri(string value) =>
        value.StartsWith("data:image/png;", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("data:image/jpeg;", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("data:image/gif;", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("data:image/webp;", StringComparison.OrdinalIgnoreCase);

    private static string RemoveAsciiWhitespaceAndControlCharacters(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (character > 0x20 && character != 0x7f)
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    private static byte[] WriteSvgDocument(XDocument document)
    {
        using var stream = new MemoryStream();
        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            OmitXmlDeclaration = true
        };

        using (var writer = XmlWriter.Create(stream, settings))
        {
            document.Save(writer);
        }

        return stream.ToArray();
    }

    private static bool IsAllowedSvgNamespace(string namespaceName) =>
        string.IsNullOrWhiteSpace(namespaceName) ||
        string.Equals(namespaceName, SvgNamespace, StringComparison.Ordinal);

    private static bool HasPngSignature(byte[] bytes) =>
        bytes.AsSpan().StartsWith(PngSignature);

    private static bool HasJpegSignature(byte[] bytes) =>
        bytes.Length >= 3 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff;

    private static bool HasGifSignature(byte[] bytes) =>
        bytes.Length >= 6 &&
        (Encoding.ASCII.GetString(bytes, 0, 6) == "GIF87a" ||
         Encoding.ASCII.GetString(bytes, 0, 6) == "GIF89a");

    private static bool HasWebpSignature(byte[] bytes) =>
        bytes.Length >= 12 &&
        Encoding.ASCII.GetString(bytes, 0, 4) == "RIFF" &&
        Encoding.ASCII.GetString(bytes, 8, 4) == "WEBP";
}
