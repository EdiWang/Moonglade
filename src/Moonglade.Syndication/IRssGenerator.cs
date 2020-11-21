using System.IO;
using System.Threading.Tasks;

namespace Moonglade.Syndication
{
    public interface IRssGenerator
    {
        Task WriteRssStreamAsync(Stream stream);
    }
}