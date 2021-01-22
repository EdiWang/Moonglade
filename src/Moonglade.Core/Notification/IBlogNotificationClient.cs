using System.Threading.Tasks;
using Moonglade.Pingback;

namespace Moonglade.Core.Notification
{
    public interface IBlogNotificationClient
    {
        Task TestNotificationAsync();

        Task NotifyCommentAsync(CommentPayload payload);

        Task NotifyCommentReplyAsync(CommentReplyPayload payload);

        Task NotifyPingbackAsync(PingbackRecord model);
    }
}
