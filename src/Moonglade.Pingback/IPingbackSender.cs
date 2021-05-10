using System.Threading.Tasks;

namespace Moonglade.Pingback
{
    public interface IPingbackSender
    {
        Task TrySendPingAsync(string postUrl, string postContent);
    }
}