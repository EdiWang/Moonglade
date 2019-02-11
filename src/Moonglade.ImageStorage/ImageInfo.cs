namespace Moonglade.ImageStorage
{
    public class ImageInfo
    {
        public byte[] ImageBytes { get; set; }

        public string ImageExtensionName { get; set; }

        public string ImageContentType => $"image/{ImageExtensionName}";
    }
}
