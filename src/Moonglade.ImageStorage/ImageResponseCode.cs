namespace Moonglade.ImageStorage
{
    public enum ImageResponseCode
    {
        GeneralException = 0,
        ImageNotExistInAzureBlob = 1,
        ImageNotExistInFileSystem = 2,
        ExtensionNameIsNull = 3
    }
}
