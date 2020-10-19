using System.Threading.Tasks;

namespace Moonglade.Syndication
{
    public interface IRssGenerator
    {
        Task WriteRss20FileAsync(string path);
    }
}