namespace Moonglade.ImageStorage.FileSystem
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
