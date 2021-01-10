using System.IO;
using System.Threading.Tasks;

namespace Moonglade.Syndication
{
    public interface IAtomGenerator
    {
        Task<string> WriteAtomAsync();
    }
}