using System;
using System.Threading.Tasks;
using Moonglade.Model;
using Moonglade.Pingback;

namespace Moonglade.Core.Notification
{
    public interface IBlogNotificationClient
    {
        Task TestNotificationAsync();

        Task NotifyCommentAsync(CommentDetailedItem model, Func<string, string> contentFormat);

        Task NotifyCommentReplyAsync(CommentReplyDetail model, string postLink);

        Task NotifyPingbackAsync(PingbackHistory model);
    }
}
