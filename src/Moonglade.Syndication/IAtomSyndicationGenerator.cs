using System.Threading.Tasks;

namespace Edi.SyndicationFeedGenerator
{
    public interface IAtomSyndicationGenerator
    {
        Task WriteAtom10FileAsync(string path);
    }
}