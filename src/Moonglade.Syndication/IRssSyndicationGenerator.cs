using System.Threading.Tasks;

namespace Edi.SyndicationFeedGenerator
{
    public interface IRssSyndicationGenerator
    {
        Task WriteRss20FileAsync(string path);
    }
}