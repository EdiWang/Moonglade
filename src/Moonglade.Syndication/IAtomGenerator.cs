using System.Threading.Tasks;

namespace Moonglade.Syndication
{
    public interface IAtomGenerator
    {
        Task WriteAtomFileAsync(string path);
    }
}