namespace Moonglade.ImageStorage;

public interface IFileNameGenerator
{
    string GetFileName(string fileName, string appendixName = "");
}
