namespace Moonglade.ImageStorage.Providers
{
    public class FileSystemImageConfiguration
    {
        public string Path { get; set; }

        public FileSystemImageConfiguration(string path)
        {
            Path = path;
        }
    }
}
