namespace Moonglade.ImageStorage;

public interface IBlogImageStorage
{
    string Name { get; }

    Task<string> InsertAsync(string fileName, byte[] imageBytes);

    Task<ImageInfo> GetAsync(string fileName);

    Task DeleteAsync(string fileName);
}