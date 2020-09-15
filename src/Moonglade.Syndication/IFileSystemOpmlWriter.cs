using System.Threading.Tasks;

namespace Moonglade.Syndication
{
    public interface IFileSystemOpmlWriter
    {
        Task WriteOpmlFileAsync(string opmlFilePath, OpmlDoc opmlDoc);
    }
}