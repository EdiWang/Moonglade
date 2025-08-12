namespace Moonglade.ImageStorage;

public class GuidFileNameGenerator(Guid id) : IFileNameGenerator
{
    public string Name => nameof(GuidFileNameGenerator);

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
        return $"img-{UniqueId:N}{appendix}{ext}".ToLowerInvariant();
    }
}