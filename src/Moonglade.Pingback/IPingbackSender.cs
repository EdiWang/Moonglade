using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Moonglade.Pingback
{
    public interface IPingbackSender
    {
        Task TrySendPingAsync(string postUrl, string postContent);
    }
}