using System.Threading.Tasks;

namespace Moonglade.Syndication
{
    public interface IAtomGenerator
    {
        Task WriteAtom10FileAsync(string path);
    }
}