namespace Moonglade.ImageStorage;

public class DatedGuidFileNameGenerator(Guid id) : IFileNameGenerator
{
    public string Name => nameof(DatedGuidFileNameGenerator);

    public Guid UniqueId { get; } = id;

    public string GetFileName(string fileName, string appendixName = "")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var ext = Path.GetExtension(fileName);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

        if (string.IsNullOrEmpty(ext))
        {
            throw new ArgumentException("File must have an extension.", nameof(fileName));
        }

        if (string.IsNullOrWhiteSpace(fileNameWithoutExtension))
        {
            throw new ArgumentException("File must have a valid name.", nameof(fileName));
        }

        var appendix = string.IsNullOrWhiteSpace(appendixName) ? string.Empty : $"-{appendixName}";
        var dateStr = DateTime.UtcNow.ToString("yyyyMMdd");
        var shortGuid = UniqueId.ToString("N")[..8]; // Use first 8 characters of GUID

        return $"{dateStr}-{shortGuid}{appendix}{ext}".ToLowerInvariant();
    }
}