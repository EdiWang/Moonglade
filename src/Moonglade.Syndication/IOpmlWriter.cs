using System.Threading.Tasks;

namespace Moonglade.Syndication
{
    public interface IOpmlWriter
    {
        Task<byte[]> GetOpmlStreamDataAsync(OpmlDoc opmlDoc);
    }
}