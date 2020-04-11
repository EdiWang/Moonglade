using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Edi.Blog.Pingback
{
    public interface IPingbackSender
    {
        ILogger<PingbackSender> Logger { get; set; }
        Task TrySendPingAsync(string postUrl, string postContent);
    }
}