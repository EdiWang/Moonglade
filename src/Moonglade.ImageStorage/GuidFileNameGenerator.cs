﻿namespace Moonglade.ImageStorage;

public class GuidFileNameGenerator(Guid id) : IFileNameGenerator
{
    public string Name => nameof(GuidFileNameGenerator);

    public Guid UniqueId { get; } = id;

    public string GetFileName(string fileName, string appendixName = "")
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentNullException(nameof(fileName));
        }

        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(ext) ||
            string.IsNullOrWhiteSpace(Path.GetFileNameWithoutExtension(fileName)))
        {
            throw new ArgumentException("Invalid File Name", nameof(fileName));
        }

        var newFileName = $"img-{UniqueId}{(string.IsNullOrWhiteSpace(appendixName) ? string.Empty : "-" + appendixName)}{ext}".ToLower();
        return newFileName;
    }
}