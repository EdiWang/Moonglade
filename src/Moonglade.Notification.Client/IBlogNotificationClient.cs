using System.Threading.Tasks;

namespace Moonglade.Notification.Client
{
    public interface IBlogNotificationClient
    {
        Task TestNotificationAsync();

        Task NotifyCommentAsync(CommentPayload payload);

        Task NotifyCommentReplyAsync(CommentReplyPayload payload);

        Task NotifyPingbackAsync(PingPayload model);
    }
}
