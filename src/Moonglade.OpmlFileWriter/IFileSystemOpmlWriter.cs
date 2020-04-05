using System.Threading.Tasks;

namespace Moonglade.OpmlFileWriter
{
    public interface IFileSystemOpmlWriter
    {
        Task WriteOpmlFileAsync(string opmlFilePath, OpmlInfo opmlInfo);
    }
}