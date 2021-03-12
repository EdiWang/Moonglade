using System;
using System.Threading.Tasks;

namespace Moonglade.Notification.Client
{
    public interface IBlogNotificationClient
    {
        Task TestNotificationAsync();

        Task NotifyCommentAsync(string username, string email, string ipAddress, string postTitle, string commentContent, DateTime createTimeUtc);

        Task NotifyCommentReplyAsync(CommentReplyPayload payload);

        Task NotifyPingbackAsync(PingPayload model);
    }
}
