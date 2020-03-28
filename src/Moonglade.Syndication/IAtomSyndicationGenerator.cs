using System.Threading.Tasks;

namespace Moonglade.Syndication
{
    public interface IAtomSyndicationGenerator
    {
        Task WriteAtom10FileAsync(string path);
    }
}