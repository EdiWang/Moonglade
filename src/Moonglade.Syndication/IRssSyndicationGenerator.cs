using System.Threading.Tasks;

namespace Moonglade.Syndication
{
    public interface IRssSyndicationGenerator
    {
        Task WriteRss20FileAsync(string path);
    }
}