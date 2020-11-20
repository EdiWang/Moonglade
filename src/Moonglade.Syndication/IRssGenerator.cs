using System.IO;
using System.Threading.Tasks;

namespace Moonglade.Syndication
{
    public interface IRssGenerator
    {
        Task WriteRssFileAsync(string path);

        Task WriteRssStreamAsync(Stream stream);
    }
}