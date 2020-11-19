using System.Threading.Tasks;

namespace Moonglade.Syndication
{
    public interface IMemoryStreamOpmlWriter
    {
        Task<byte[]> WriteOpmlStreamAsync(OpmlDoc opmlDoc);
    }
}