using System.Threading.Tasks;

namespace Moonglade.Syndication
{
    public interface IOpmlWriter
    {
        Task<string> GetOpmlDataAsync(OpmlDoc opmlDoc);
    }
}