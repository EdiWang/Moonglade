namespace Moonglade.ImageStorage;

public interface IBlogImageStorage
{
    string Name { get; }

    Task<string> InsertAsync(string fileName, byte[] imageBytes);

    Task<string> InsertSecondaryAsync(string fileName, byte[] imageBytes);

    Task<ImageInfo> GetInfoAsync(string fileName);

    Task<Stream> OpenReadAsync(string fileName);

    Task DeleteAsync(string fileName);
}
