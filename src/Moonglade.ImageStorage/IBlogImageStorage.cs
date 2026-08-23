namespace Moonglade.ImageStorage;

public interface IBlogImageStorage
{
    Task<string> InsertAsync(string fileName, byte[] imageBytes);

    Task<string> InsertOriginalAsync(string fileName, byte[] imageBytes);

    Task<ImageInfo> GetInfoAsync(string fileName);

    Task<Stream> OpenReadAsync(string fileName);

    Task DeleteAsync(string fileName);
}
